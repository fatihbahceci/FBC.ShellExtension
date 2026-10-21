using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using FBC.ShellExtension.Core.Resources;
using static FBC.ShellExtension.Runner.Interop.Native;
using static FBC.ShellExtension.Runner.UI.RunnerWindow;

namespace FBC.ShellExtension.Runner.UI;

/// <summary>What a <see cref="TextPrompt"/> asks and how it checks the answer.</summary>
/// <param name="Title">Window title.</param>
/// <param name="Question">The label above the field; may wrap to two lines.</param>
/// <param name="Text">Initial text, selected so typing replaces it.</param>
/// <param name="Hint">Grey text under the field; null for none.</param>
/// <param name="Choices">
/// Presets: the field is an editable combo box. Null or empty: a plain single-line edit box,
/// no drop-down.
/// </param>
/// <param name="OkText">The default button's text; null means <see cref="Strings.PromptOk"/>.</param>
/// <param name="Validate">
/// Called with the trimmed text on OK; returns null when it is fine, else a message shown in a
/// warning box owned by the prompt, which then stays open with the field focused and its text
/// selected. Null: anything is accepted, the empty string too.
/// </param>
/// <param name="Secret">The field shows dots instead of what is typed (an API key); no choices then.</param>
internal sealed record TextPromptOptions(string Title, string Question, string Text, string? Hint = null,
    IReadOnlyList<string>? Choices = null, string? OkText = null, Func<string, string?>? Validate = null, bool Secret = false);

/// <summary>
/// Asks for a line of text before a built-in tool starts: a small window, raw Win32 like
/// <see cref="RunnerWindow"/>, shown on the runner's thread before the progress window exists.
/// </summary>
/// <remarks>
/// It has no owner, so being modal means a message loop of its own, which ends when the window
/// is destroyed. It never posts WM_QUIT: the progress window's loop runs next on this thread,
/// and a WM_QUIT left behind would end that loop before its window was drawn. One prompt at a
/// time: the window procedure finds it through a static field.
/// </remarks>
internal sealed unsafe class TextPrompt
{
    private const string ClassName = "FBCTools.TextPrompt";

    // The question fits two lines; a name in it is shortened to keep it there.
    private const int MaxShownName = 40;

    // The buttons are IDOK and IDCANCEL, so IsDialogMessage turns Enter and Esc into them.
    private const int IdQuestion = 101;
    private const int IdField = 102;
    private const int IdHint = 103;

    private const uint Style = WS_CAPTION | WS_SYSMENU;
    private const uint ExStyle = WS_EX_DLGMODALFRAME;
    private const uint TextStyle = WS_CHILD | WS_VISIBLE | SS_NOPREFIX | SS_EDITCONTROL;

    // Room for a two-line question, the field and a two-line hint above the buttons.
    private const int ClientWidth = 420;
    private const int ClientHeight = 150;

    // A combo box of short presets keeps this width; longer ones widen it up to the full width.
    private const int MinComboWidth = 180;

    private static TextPrompt? s_prompt;
    private static bool s_registered;

    private readonly TextPromptOptions _options;
    private readonly IReadOnlyList<string> _choices;

    private nint _hwnd;
    private nint _label;
    private nint _field;
    private nint _hint;
    private nint _ok;
    private nint _cancel;
    private nint _font;
    private nint _focus;

    private bool _destroying;
    private bool _destroyed;
    private string? _result;
    private Exception? _fatal;

    private TextPrompt(TextPromptOptions options)
    {
        _options = options;
        _choices = options.Secret ? [] : options.Choices ?? [];
    }

    private bool IsCombo => _choices.Count > 0;

    /// <returns>The trimmed text, or null when the user cancelled.</returns>
    /// <remarks>An exception thrown by <see cref="TextPromptOptions.Validate"/> closes the prompt and is thrown again here.</remarks>
    /// <exception cref="Win32Exception">The window could not be created.</exception>
    /// <exception cref="InvalidOperationException">Another prompt is open.</exception>
    /// <summary>
    /// A name to put in a question: a long one keeps its start and its end, where the extension
    /// and any number are, so the question stays within its two lines.
    /// </summary>
    public static string ShortName(string name) =>
        name.Length <= MaxShownName ? name : $"{name[..(MaxShownName / 2 - 1)]}…{name[^(MaxShownName / 2)..]}";

    public static string? Ask(TextPromptOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (s_prompt is not null)
        {
            throw new InvalidOperationException("A text prompt is already open.");
        }

        // The progress window does this too, but the prompt may be the first window shown.
        var controls = new INITCOMMONCONTROLSEX
        {
            dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
            dwICC = ICC_STANDARD_CLASSES,
        };
        InitCommonControlsEx(&controls);

        nint instance = GetModuleHandleW(0);
        Register(instance);

        var prompt = new TextPrompt(options);
        s_prompt = prompt;
        try
        {
            return prompt.Run(instance);
        }
        finally
        {
            s_prompt = null;
        }
    }

    private static void Register(nint instance)
    {
        if (s_registered)
        {
            return;
        }

        fixed (char* className = ClassName)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WndProc,
                hInstance = instance,
                hIcon = LoadIconW(0, IDI_APPLICATION),
                hCursor = LoadCursorW(0, IDC_ARROW),
                hbrBackground = COLOR_BTNFACE + 1,
                lpszClassName = className,
            };
            if (RegisterClassExW(&windowClass) == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }

        s_registered = true;
    }

    private string? Run(nint instance)
    {
        // Created where it will be shown, at that monitor's DPI, so no DPI change follows.
        RECT frame = InitialFrame(ClientWidth, ClientHeight, Style, ExStyle);

        // WM_CREATE runs inside this call and creates the child controls.
        nint hwnd = CreateWindowExW(ExStyle, ClassName, _options.Title, Style,
            frame.left, frame.top, frame.right - frame.left, frame.bottom - frame.top, 0, 0, instance, 0);
        if (hwnd == 0)
        {
            ExceptionDispatchInfo.Throw(_fatal ?? new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        ShowWindow(hwnd, SW_SHOWNORMAL);
        UpdateWindow(hwnd);
        FocusField();

        MSG msg;
        while (!_destroyed)
        {
            // 0 is WM_QUIT. Nothing in the runner posts one; one that comes anyway cancels the
            // prompt and is not posted again, so it cannot end the progress window's loop.
            int result = GetMessageW(&msg, 0, 0, 0);
            if (result == 0 || result == -1)
            {
                break;
            }

            if (!IsDialogMessageW(hwnd, &msg))
            {
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
        }

        if (!_destroyed)
        {
            _result = null;
            DestroyWindow(hwnd);
        }

        if (_fatal is not null)
        {
            ExceptionDispatchInfo.Throw(_fatal);
        }

        return _result;
    }

    [UnmanagedCallersOnly]
    private static nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        TextPrompt? prompt = s_prompt;
        try
        {
            return prompt is not null
                ? prompt.Handle(hwnd, msg, wParam, lParam)
                : DefWindowProcW(hwnd, msg, wParam, lParam);
        }
        catch (Exception ex)
        {
            // Nothing may cross this boundary: under NativeAOT an exception here ends the
            // process on the spot. Close the prompt, unanswered; Ask throws the error again.
            // Not PostQuitMessage, as RunnerWindow does: see the remarks on the class.
            if (prompt is null)
            {
                return 0;
            }

            prompt._fatal ??= ex;
            if (msg == WM_CREATE)
            {
                return -1;
            }

            prompt.Close();
            return 0;
        }
    }

    private nint Handle(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WM_CREATE:
                _hwnd = hwnd;
                return CreateChildren() ? 0 : -1;

            case WM_SIZE:
                Layout();
                return 0;

            case WM_DPICHANGED:
                var suggested = (RECT*)lParam;
                SetWindowPos(hwnd, 0, suggested->left, suggested->top,
                    suggested->right - suggested->left, suggested->bottom - suggested->top, SWP_NOZORDER | SWP_NOACTIVATE);
                ApplyFont();
                Layout();
                return 0;

            // As in a dialog box: the focus leaves with the window and comes back where it was,
            // to the field the first time.
            case WM_ACTIVATE:
                if ((wParam & 0xFFFF) == WA_INACTIVE)
                {
                    _focus = GetFocus();
                }
                else
                {
                    SetFocus(_focus != 0 && IsChild(hwnd, _focus) ? _focus : _field);
                }

                return 0;

            // The default background, grey text.
            case WM_CTLCOLORSTATIC when _hint != 0 && lParam == _hint:
                nint brush = DefWindowProcW(hwnd, msg, wParam, lParam);
                SetTextColor(wParam, GetSysColor(COLOR_GRAYTEXT));
                return brush;

            case WM_COMMAND:
                OnCommand((int)(wParam & 0xFFFF), (int)((wParam >> 16) & 0xFFFF));
                return 0;

            case WM_DESTROY:
                _destroying = true;
                return 0;

            case WM_NCDESTROY:
                _destroyed = true;

                // Destroyed by a message sent from another thread, GetMessageW would go on
                // waiting for a posted message that never comes; this one wakes the loop. It is
                // harmless when the loop has already ended: a thread message nobody dispatches.
                PostThreadMessageW(GetCurrentThreadId(), WM_NULL, 0, 0);
                if (_font != 0)
                {
                    DeleteObject(_font);
                    _font = 0;
                }

                break;
        }

        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private bool CreateChildren()
    {
        nint instance = GetModuleHandleW(0);

        // Created in tab order; the labels are no tab stops. A single-line edit leaves Enter to
        // IsDialogMessage, as the combo box does while its list is closed.
        return Create(ref _label, 0, "STATIC", _options.Question, TextStyle, IdQuestion)
            && (IsCombo
                ? Create(ref _field, 0, "COMBOBOX", null, WS_CHILD | WS_VISIBLE | WS_TABSTOP | WS_VSCROLL | CBS_DROPDOWN | CBS_AUTOHSCROLL, IdField)
                : Create(ref _field, WS_EX_CLIENTEDGE, "EDIT", null,
                    WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_AUTOHSCROLL | (_options.Secret ? ES_PASSWORD : 0), IdField))
            && (_options.Hint is null || Create(ref _hint, 0, "STATIC", _options.Hint, TextStyle, IdHint))
            && Create(ref _ok, 0, "BUTTON", _options.OkText ?? Strings.PromptOk(), WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_DEFPUSHBUTTON, IDOK)
            && Create(ref _cancel, 0, "BUTTON", Strings.PromptCancel(), WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON, IDCANCEL)
            && Configure();

        bool Create(ref nint control, uint exStyle, string className, string? text, uint style, int id)
        {
            control = CreateWindowExW(exStyle, className, text, style, 0, 0, 0, 0, _hwnd, id, instance, 0);
            if (control == 0)
            {
                _fatal = new Win32Exception(Marshal.GetLastPInvokeError(), $"{className}: {new Win32Exception(Marshal.GetLastPInvokeError()).Message}");
                return false;
            }

            return true;
        }

        bool Configure()
        {
            int selected = -1;
            for (int i = 0; i < _choices.Count; i++)
            {
                fixed (char* text = _choices[i])
                {
                    SendMessageW(_field, CB_ADDSTRING, 0, (nint)text);
                }

                if (selected < 0 && string.Equals(_choices[i], _options.Text, StringComparison.Ordinal))
                {
                    selected = i;
                }
            }

            // A preset is selected in the list too, so the drop-down opens on it.
            if (selected >= 0)
            {
                SendMessageW(_field, CB_SETCURSEL, selected, 0);
            }
            else
            {
                SetWindowTextW(_field, _options.Text);
            }

            ApplyFont();
            Layout();
            return true;
        }
    }

    private void Layout()
    {
        if (_cancel == 0)
        {
            return;
        }

        RECT client;
        GetClientRect(_hwnd, &client);
        uint dpi = DpiOf(_hwnd);
        int margin = Scale(12, dpi);
        int gap = Scale(8, dpi);
        int buttonWidth = Scale(96, dpi);
        int buttonHeight = Scale(28, dpi);
        int width = Math.Max(0, client.right - 2 * margin);
        const uint flags = SWP_NOZORDER | SWP_NOACTIVATE;

        int y = margin;
        int questionHeight = TextHeight(_label, _options.Question, width, Scale(32, dpi));
        SetWindowPos(_label, 0, margin, y, width, questionHeight, flags);
        y += questionHeight + gap / 2;

        if (IsCombo)
        {
            // A drop-down combo box takes its height from its font; the height given is the list's.
            SetWindowPos(_field, 0, margin, y, ComboWidth(width, dpi), Scale(200, dpi), flags);
        }
        else
        {
            // As WinForms sizes a TextBox: a line of text and seven pixels of border and padding.
            // The full width, so a command line of arguments shows a good part of itself.
            SetWindowPos(_field, 0, margin, y, width, TextHeight(_field, "Ag", width, Scale(16, dpi)) + Scale(7, dpi), flags);
        }

        RECT field;
        GetWindowRect(_field, &field);
        y += field.bottom - field.top + gap / 2;
        if (_hint != 0)
        {
            string hint = _options.Hint ?? "";
            SetWindowPos(_hint, 0, margin, y, width, TextHeight(_hint, hint, width, Scale(16, dpi)), flags);
        }

        int buttonTop = client.bottom - margin - buttonHeight;
        int cancelLeft = client.right - margin - buttonWidth;
        SetWindowPos(_ok, 0, cancelLeft - gap - buttonWidth, buttonTop, buttonWidth, buttonHeight, flags);
        SetWindowPos(_cancel, 0, cancelLeft, buttonTop, buttonWidth, buttonHeight, flags);
    }

    /// <summary>
    /// Wide enough for the longest preset and the initial text, the drop-down button besides:
    /// never narrower than <see cref="MinComboWidth"/>, never wider than the window. A longer
    /// typed text scrolls.
    /// </summary>
    private int ComboWidth(int width, uint dpi)
    {
        int widest = TextWidth(_field, _options.Text);
        foreach (string choice in _choices)
        {
            widest = Math.Max(widest, TextWidth(_field, choice));
        }

        // The drop-down button, the borders and the edit's margins.
        return Math.Min(width, Math.Max(Scale(MinComboWidth, dpi), widest + Scale(32, dpi)));
    }

    /// <summary>
    /// The height <paramref name="text"/> wraps to in a label of <paramref name="width"/>,
    /// measured as the label draws it, so a one-line question leaves no gap above the field.
    /// </summary>
    private int TextHeight(nint label, string text, int width, int fallback)
    {
        int height = Measure(label, text, width, DT_WORDBREAK | DT_EXPANDTABS | DT_NOPREFIX | DT_EDITCONTROL).bottom;
        return height > 0 ? height : fallback;
    }

    /// <summary>The width of <paramref name="text"/> on one line; 0 when it cannot be measured.</summary>
    private int TextWidth(nint control, string text) => Measure(control, text, 0, DT_SINGLELINE | DT_NOPREFIX).right;

    /// <summary>The bounds DrawTextW gives <paramref name="text"/> in the prompt's font; empty without a DC.</summary>
    private RECT Measure(nint control, string text, int width, uint format)
    {
        nint dc = GetDC(control);
        if (dc == 0)
        {
            return default;
        }

        nint previous = SelectObject(dc, _font);
        var bounds = new RECT { right = width };
        fixed (char* p = text)
        {
            DrawTextW(dc, p, text.Length, &bounds, DT_CALCRECT | format);
        }

        if (previous != 0)
        {
            SelectObject(dc, previous);
        }

        ReleaseDC(control, dc);
        return bounds;
    }

    /// <summary>The icon title font at the window's DPI, as <see cref="RunnerWindow"/> uses.</summary>
    private void ApplyFont()
    {
        LOGFONTW font;
        if (!SystemParametersInfoForDpi(SPI_GETICONTITLELOGFONT, (uint)sizeof(LOGFONTW), &font, 0, DpiOf(_hwnd)))
        {
            return;
        }

        nint created = CreateFontIndirectW(&font);
        if (created == 0)
        {
            return;
        }

        foreach (nint control in (ReadOnlySpan<nint>)[_label, _field, _hint, _ok, _cancel])
        {
            if (control != 0)
            {
                SendMessageW(control, WM_SETFONT, created, 1);
            }
        }

        if (_font != 0)
        {
            DeleteObject(_font);
        }

        _font = created;
    }

    private void OnCommand(int id, int code)
    {
        if (code != BN_CLICKED)
        {
            return;
        }

        if (id == IDOK)
        {
            Accept();
        }
        else if (id == IDCANCEL)
        {
            Close();
        }
    }

    /// <summary>Closes with the text, or says what is wrong with it and leaves the prompt open.</summary>
    private void Accept()
    {
        string text = FieldText().Trim();
        if (_options.Validate?.Invoke(text) is not { } problem)
        {
            _result = text;
            Close();
            return;
        }

        MessageBoxW(_hwnd, problem, _options.Title, MB_ICONWARNING);
        FocusField();
    }

    private void Close()
    {
        if (!_destroying)
        {
            DestroyWindow(_hwnd);
        }
    }

    /// <summary>The focus in the field with all of its text selected, so that typing replaces it.</summary>
    private void FocusField()
    {
        SetFocus(_field);
        if (IsCombo)
        {
            // Start 0 in the low word, end -1 (the end of the text) in the high word.
            uint all = 0xFFFF0000;
            SendMessageW(_field, CB_SETEDITSEL, 0, (nint)all);
        }
        else
        {
            // From 0 to -1, the end of the text.
            SendMessageW(_field, EM_SETSEL, 0, -1);
        }
    }

    private string FieldText()
    {
        int length = GetWindowTextLengthW(_field);
        if (length <= 0)
        {
            return "";
        }

        var buffer = new char[length + 1];
        fixed (char* p = buffer)
        {
            return new string(p, 0, GetWindowTextW(_field, p, buffer.Length));
        }
    }
}
