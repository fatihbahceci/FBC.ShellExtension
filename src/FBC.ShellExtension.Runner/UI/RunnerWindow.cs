using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Execution;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using FBC.ShellExtension.Runner.Execution;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.UI;

/// <summary>
/// The progress window for track mode: raw Win32, one per process.
/// </summary>
/// <remarks>
/// Threading (ADR-0006): the job runs on the thread pool and never touches a window. Its
/// events go into a queue that a 100 ms timer applies on the UI thread; a timer, not a posted
/// message, because posted messages are served before input and painting, and a program that
/// writes without pause would otherwise starve the Cancel button. WM_APP_DONE says the job task
/// has ended. Only the UI thread reads or writes the state below, and closing the window while
/// the job runs takes the same confirmed cancel path as the Cancel button.
/// </remarks>
internal sealed unsafe class RunnerWindow : IExecutionObserver
{
    private const string ClassName = "FBCTools.Runner";

    // The button is IDCANCEL, so Esc presses it through IsDialogMessage. The other ids stay
    // clear of IDOK and IDCANCEL.
    private const int IdStatus = 101;
    private const int IdProgress = 102;
    private const int IdLog = 103;

    private const uint WM_APP_DONE = WM_APP + 1;
    private const nuint CloseTimerId = 1;
    private const nuint DrainTimerId = 2;
    private const uint CloseDelayMs = 1500;
    private const uint DrainIntervalMs = 100;

    // An edit control slows down as it grows; past this the oldest half is dropped.
    private const int MaxLogChars = 1_000_000;

    // The bar counts each item in thousandths, so a tool reading a large file moves it within
    // the item's step. Commands report no progress and still move it a whole step at a time.
    private const int StepScale = 1000;

    private const int ClientWidth = 640;
    private const int ClientHeight = 400;

    private static RunnerWindow? s_window;

    private enum State
    {
        Running,
        CancelRequested,
        Done,
    }

    private abstract record UiEvent;

    private sealed record StatusEvent(string Text) : UiEvent;

    private sealed record StartedEvent(int Index) : UiEvent;

    private sealed record OutputEvent(int Index, string Line) : UiEvent;

    private sealed record ProgressEvent(int Index, long Done, long Total) : UiEvent;

    private sealed record FinishedEvent(ProcessResult Result) : UiEvent;

    private readonly RunJob _job;
    private readonly IReadOnlyList<PlannedProcess> _plan;
    private readonly string? _versionMismatch;
    private readonly int _stepScale;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<UiEvent> _events = new();

    private nint _hwnd;
    private nint _status;
    private nint _progress;
    private nint _log;
    private nint _button;
    private nint _font;

    private State _state = State.Running;
    private bool _closeWhenDone;
    private bool _confirming;
    private int _finished;
    private int _running = -1;
    private int _runningPart;
    private string _current = "";
    private string? _statusOverride;
    private Task<JobSummary>? _task;
    private Exception? _fatal;
    private int _exitCode = 1;

    public RunnerWindow(RunJob job, IReadOnlyList<PlannedProcess> plan, string? versionMismatch)
    {
        _job = job;
        _plan = plan;
        _versionMismatch = versionMismatch;

        // PBM_SETRANGE32 takes an int; only a selection of millions of items needs fewer steps.
        _stepScale = Math.Clamp(int.MaxValue / Math.Max(1, plan.Count), 1, StepScale);
    }

    /// <summary>For the elevated runner's "waiting for approval"; any thread.</summary>
    public void Status(string text) => _events.Enqueue(new StatusEvent(text));

    void IExecutionObserver.Started(int index, PlannedProcess process) => _events.Enqueue(new StartedEvent(index));

    void IExecutionObserver.Output(int index, string line) => _events.Enqueue(new OutputEvent(index, line));

    void IExecutionObserver.Progress(int index, long done, long total) => _events.Enqueue(new ProgressEvent(index, done, total));

    void IExecutionObserver.Finished(ProcessResult result) => _events.Enqueue(new FinishedEvent(result));

    /// <returns>The process exit code: 0 when every process succeeded.</returns>
    public int Run(IJobRunner runner)
    {
        s_window = this;
        var controls = new INITCOMMONCONTROLSEX
        {
            dwSize = (uint)sizeof(INITCOMMONCONTROLSEX),
            dwICC = ICC_STANDARD_CLASSES | ICC_PROGRESS_CLASS,
        };
        InitCommonControlsEx(&controls);

        nint instance = GetModuleHandleW(0);
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
                return ShowFatal(new Win32Exception(Marshal.GetLastPInvokeError()));
            }
        }

        // Created where it will be shown, at that monitor's DPI, so no DPI change follows.
        RECT frame = InitialFrame(ClientWidth, ClientHeight, WS_OVERLAPPEDWINDOW, 0);

        // WM_CREATE runs inside this call and creates the child controls.
        nint hwnd = CreateWindowExW(0, ClassName, Strings.RunnerWindowTitle(_job.Label), WS_OVERLAPPEDWINDOW,
            frame.left, frame.top, frame.right - frame.left, frame.bottom - frame.top, 0, 0, instance, 0);
        if (hwnd == 0)
        {
            return ShowFatal(_fatal ?? new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        ShowWindow(_hwnd, SW_SHOWNORMAL);
        UpdateWindow(_hwnd);
        SetFocus(_button);

        if (_versionMismatch is not null)
        {
            AppendLog(_versionMismatch + "\r\n");
        }

        SetTimer(_hwnd, DrainTimerId, DrainIntervalMs, 0);
        _task = Task.Run(() => runner.RunAsync(_job.Command, _plan, this, _cts.Token));
        _task.ContinueWith(_ => PostMessageW(_hwnd, WM_APP_DONE, 0, 0), CancellationToken.None,
            TaskContinuationOptions.None, TaskScheduler.Default);

        MSG msg;
        int result;
        while ((result = GetMessageW(&msg, 0, 0, 0)) != 0 && result != -1)
        {
            if (HandleLogKey(&msg))
            {
                continue;
            }

            if (!IsDialogMessageW(_hwnd, &msg))
            {
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
        }

        // Nothing pumps messages any more, so waiting here cannot leave a hung window behind.
        if (IsWindow(_hwnd))
        {
            DestroyWindow(_hwnd);
        }

        if (!_task.IsCompleted)
        {
            _cts.Cancel();
            try
            {
                _task.Wait(TimeSpan.FromSeconds(10));
            }
            catch (AggregateException)
            {
            }
        }

        return _fatal is not null ? ShowFatal(_fatal) : _exitCode;
    }

    [UnmanagedCallersOnly]
    private static nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        RunnerWindow? window = s_window;
        try
        {
            return window is not null
                ? window.Handle(hwnd, msg, wParam, lParam)
                : DefWindowProcW(hwnd, msg, wParam, lParam);
        }
        catch (Exception ex)
        {
            // Nothing may cross this boundary: under NativeAOT an exception here ends the
            // process on the spot. Stop the job and leave the loop; Run reports the error.
            if (window is not null)
            {
                window._fatal ??= ex;
                window._cts.Cancel();
            }

            PostQuitMessage(3);
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

            case WM_GETMINMAXINFO:
                var limits = (MINMAXINFO*)lParam;
                uint dpi = DpiOf(hwnd);
                limits->ptMinTrackSize = new POINT { x = Scale(420, dpi), y = Scale(260, dpi) };
                return 0;

            case WM_DPICHANGED:
                var suggested = (RECT*)lParam;
                SetWindowPos(hwnd, 0, suggested->left, suggested->top,
                    suggested->right - suggested->left, suggested->bottom - suggested->top, SWP_NOZORDER | SWP_NOACTIVATE);
                ApplyFont();
                Layout();
                return 0;

            case WM_COMMAND:
                OnCommand((int)(wParam & 0xFFFF), (int)((wParam >> 16) & 0xFFFF));
                return 0;

            // A multiline edit asks its parent to move the focus on Tab; the button is the only
            // other stop.
            case WM_NEXTDLGCTL:
                SetFocus(_button);
                return 0;

            case WM_CLOSE:
                OnCloseRequested();
                return 0;

            case WM_TIMER when (nuint)wParam == DrainTimerId:
                Drain();
                return 0;

            case WM_TIMER when (nuint)wParam == CloseTimerId:
                KillTimer(hwnd, CloseTimerId);
                DestroyWindow(hwnd);
                return 0;

            case WM_APP_DONE:
                OnDone();
                return 0;

            case WM_DESTROY:
                KillTimer(hwnd, CloseTimerId);
                KillTimer(hwnd, DrainTimerId);
                PostQuitMessage(_exitCode);
                return 0;

            case WM_NCDESTROY:
                if (_font != 0)
                {
                    DeleteObject(_font);
                    _font = 0;
                }

                break;
        }

        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    /// <summary>
    /// Keys aimed at the log. A multiline edit takes Esc and Tab for itself and, inside a
    /// dialog-like window, turns Esc into WM_CLOSE; here Esc means the button, Tab leaves the
    /// log, and Enter closes a finished window, wherever the focus is.
    /// </summary>
    private bool HandleLogKey(MSG* msg)
    {
        if (msg->message != WM_KEYDOWN || msg->hwnd != _log || _log == 0)
        {
            return false;
        }

        switch (msg->wParam)
        {
            case VK_ESCAPE:
                SendMessageW(_hwnd, WM_COMMAND, IDCANCEL, _button);
                return true;
            case VK_TAB:
                SetFocus(_button);
                return true;
            case VK_RETURN when _state == State.Done:
                SendMessageW(_hwnd, WM_COMMAND, IDOK, 0);
                return true;
            default:
                return false;
        }
    }

    private bool CreateChildren()
    {
        nint instance = GetModuleHandleW(0);
        return Create(ref _status, 0, "STATIC", Strings.RunnerStarting(), WS_CHILD | WS_VISIBLE | SS_NOPREFIX | SS_ENDELLIPSIS, IdStatus)
            && Create(ref _progress, 0, "msctls_progress32", null, WS_CHILD | WS_VISIBLE, IdProgress)
            && Create(ref _log, WS_EX_CLIENTEDGE, "EDIT", null,
                WS_CHILD | WS_VISIBLE | WS_VSCROLL | ES_MULTILINE | ES_READONLY | ES_AUTOVSCROLL | ES_NOHIDESEL, IdLog)
            && Create(ref _button, 0, "BUTTON", Strings.RunnerCancel(), WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_DEFPUSHBUTTON, IDCANCEL)
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
            // The default limit (about 32,000 characters) would silently drop the end of a long
            // log, the summary included; 0 lifts it, and MaxLogChars keeps it in check.
            SendMessageW(_log, EM_SETLIMITTEXT, 0, 0);
            SendMessageW(_progress, PBM_SETRANGE32, 0, Math.Max(1, _plan.Count) * _stepScale);
            ApplyFont();
            return true;
        }
    }

    /// <summary>
    /// Centred on the monitor under the cursor, where the user just right-clicked, at its DPI.
    /// The client size is in logical pixels; <see cref="TextPrompt"/> uses it too.
    /// </summary>
    internal static RECT InitialFrame(int clientWidth, int clientHeight, uint style, uint exStyle)
    {
        POINT cursor;
        GetCursorPos(&cursor);
        nint monitor = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);
        uint dpiX = 96;
        uint dpiY = 96;
        if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, &dpiX, &dpiY) < 0 || dpiX == 0)
        {
            dpiX = 96;
        }

        var frame = new RECT { right = Scale(clientWidth, dpiX), bottom = Scale(clientHeight, dpiX) };
        AdjustWindowRectExForDpi(&frame, style, false, exStyle, dpiX);
        int width = frame.right - frame.left;
        int height = frame.bottom - frame.top;

        var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        if (!GetMonitorInfoW(monitor, &info))
        {
            return new RECT { left = CW_USEDEFAULT, top = CW_USEDEFAULT, right = CW_USEDEFAULT + width, bottom = CW_USEDEFAULT + height };
        }

        RECT work = info.rcWork;
        int x = work.left + Math.Max(0, (work.right - work.left - width) / 2);
        int y = work.top + Math.Max(0, (work.bottom - work.top - height) / 2);
        return new RECT { left = x, top = y, right = x + width, bottom = y + height };
    }

    private void Layout()
    {
        if (_button == 0)
        {
            return;
        }

        RECT client;
        GetClientRect(_hwnd, &client);
        uint dpi = DpiOf(_hwnd);
        int margin = Scale(12, dpi);
        int gap = Scale(8, dpi);
        int statusHeight = Scale(20, dpi);
        int progressHeight = Scale(16, dpi);
        int buttonWidth = Scale(96, dpi);
        int buttonHeight = Scale(28, dpi);
        int width = Math.Max(0, client.right - 2 * margin);
        const uint flags = SWP_NOZORDER | SWP_NOACTIVATE;

        int y = margin;
        SetWindowPos(_status, 0, margin, y, width, statusHeight, flags);
        y += statusHeight + gap / 2;
        SetWindowPos(_progress, 0, margin, y, width, progressHeight, flags);
        y += progressHeight + gap;
        int buttonTop = client.bottom - margin - buttonHeight;
        SetWindowPos(_log, 0, margin, y, width, Math.Max(0, buttonTop - gap - y), flags);
        SetWindowPos(_button, 0, client.right - margin - buttonWidth, buttonTop, buttonWidth, buttonHeight, flags);
    }

    /// <summary>The icon title font at the window's DPI, as Microsoft's per-monitor sample does.</summary>
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

        foreach (nint control in (ReadOnlySpan<nint>)[_status, _log, _button])
        {
            SendMessageW(control, WM_SETFONT, created, 1);
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

        if (id == IDCANCEL)
        {
            if (_state == State.Running)
            {
                ConfirmCancel();
            }
            else if (_state == State.Done)
            {
                DestroyWindow(_hwnd);
            }
        }
        else if (id == IDOK && _state == State.Done)
        {
            DestroyWindow(_hwnd);
        }
    }

    private void OnCloseRequested()
    {
        switch (_state)
        {
            case State.Running:
                if (ConfirmCancel())
                {
                    if (_state == State.Done)
                    {
                        DestroyWindow(_hwnd);
                    }
                    else
                    {
                        _closeWhenDone = true;
                    }
                }

                break;
            case State.CancelRequested:
                _closeWhenDone = true;
                break;
            case State.Done:
                DestroyWindow(_hwnd);
                break;
        }
    }

    /// <returns>Whether the user confirmed.</returns>
    private bool ConfirmCancel()
    {
        _confirming = true;
        int answer;
        try
        {
            answer = MessageBoxW(_hwnd, Strings.RunnerConfirmCancel(), ProductInfo.Name, MB_YESNO | MB_ICONQUESTION | MB_DEFBUTTON2);
        }
        finally
        {
            _confirming = false;
        }

        // The box runs its own message loop, so the job may have finished meanwhile; if it all
        // succeeded, the close that OnDone held back while the box was open happens now.
        if (_state == State.Done)
        {
            StartCloseTimerIfDue();
        }

        if (answer != IDYES)
        {
            return false;
        }

        if (_state == State.Running)
        {
            _state = State.CancelRequested;
            _cts.Cancel();
            EnableWindow(_button, false);
            SetWindowTextW(_status, Strings.RunnerCancelling());
        }

        return true;
    }

    private void Drain()
    {
        if (_events.IsEmpty)
        {
            return;
        }

        bool parallel = _job.Command.MultiSelect == MultiSelectMode.PerFileParallel;
        var log = new StringBuilder();
        while (_events.TryDequeue(out UiEvent? uiEvent))
        {
            switch (uiEvent)
            {
                case StatusEvent status:
                    _statusOverride = status.Text;
                    break;
                case StartedEvent started:
                    _statusOverride = null;
                    _running = started.Index;
                    _runningPart = 0;
                    _current = SummaryFormatter.Subject(_plan[started.Index]);
                    if (_job.Command.ShowOutput)
                    {
                        log.Append("▶ ").Append(_current).Append("\r\n");
                    }

                    break;
                case OutputEvent output:
                    log.Append(parallel ? Path.GetFileName(SummaryFormatter.Subject(_plan[output.Index])) + "> " : "    ")
                        .Append(output.Line)
                        .Append("\r\n");
                    break;

                // Only the bar shows it, and only the newest value counts. An empty file has
                // nothing left to read.
                case ProgressEvent progress when progress.Index == _running:
                    _runningPart = progress.Total > 0
                        ? (int)(Math.Clamp(progress.Done, 0, progress.Total) * (double)_stepScale / progress.Total)
                        : _stepScale;
                    break;
                case FinishedEvent finished:
                    _finished++;
                    if (finished.Result.Index == _running)
                    {
                        _running = -1;
                        _runningPart = 0;
                    }

                    if (finished.Result.Outcome != ProcessOutcome.NotStarted || SummaryFormatter.HasOwnReason(finished.Result))
                    {
                        log.Append(ToWindowsNewLines(SummaryFormatter.Line(finished.Result))).Append("\r\n");
                    }

                    break;
            }
        }

        if (log.Length > 0)
        {
            AppendLog(log.ToString());
        }

        SendMessageW(_progress, PBM_SETPOS, Math.Min(_finished * _stepScale + _runningPart, _plan.Count * _stepScale), 0);
        if (_state == State.Running)
        {
            SetWindowTextW(_status, _statusOverride ?? Strings.RunnerRunning(_finished, _plan.Count, _current));
        }
    }

    private void OnDone()
    {
        if (_state == State.Done || _task is null)
        {
            return;
        }

        KillTimer(_hwnd, DrainTimerId);
        Drain();
        _state = State.Done;

        if (_task.IsCompletedSuccessfully)
        {
            JobSummary summary = _task.Result;
            _exitCode = summary.AllSucceeded ? 0 : 1;
            AppendLog($"\r\n── {Strings.RunnerSummaryTitle()} ──\r\n{ToWindowsNewLines(SummaryFormatter.Report(summary))}\r\n");
            SetWindowTextW(_status, SummaryFormatter.Headline(summary));
            SendMessageW(_progress, PBM_SETPOS, _plan.Count * _stepScale, 0);
            if (summary.Failed > 0)
            {
                SendMessageW(_progress, PBM_SETSTATE, PBST_ERROR, 0);
            }
            else if (!summary.AllSucceeded)
            {
                SendMessageW(_progress, PBM_SETSTATE, PBST_PAUSED, 0);
            }
        }
        else
        {
            string message = Strings.RunnerFatal(_task.Exception?.GetBaseException().Message ?? "?");
            AppendLog($"\r\n{message}\r\n");
            SetWindowTextW(_status, message);
            SendMessageW(_progress, PBM_SETSTATE, PBST_ERROR, 0);
        }

        SetWindowTextW(_button, Strings.RunnerClose());
        EnableWindow(_button, true);
        SetFocus(_button);

        if (_closeWhenDone)
        {
            DestroyWindow(_hwnd);
        }
        else
        {
            StartCloseTimerIfDue();
        }
    }

    /// <summary>
    /// closeOnSuccess, unless the user is answering the cancel question or has to see that
    /// Explorer still runs an older shell extension.
    /// </summary>
    private void StartCloseTimerIfDue()
    {
        if (_exitCode == 0 && _job.Command.CloseOnSuccess && _versionMismatch is null && !_confirming && !_closeWhenDone)
        {
            SetTimer(_hwnd, CloseTimerId, CloseDelayMs, 0);
        }
    }

    /// <summary>
    /// Appends at the end. While the user follows the end, the log scrolls with it; once they
    /// have selected something or scrolled up, their selection and view stay where they are.
    /// </summary>
    private void AppendLog(string text)
    {
        text = WithoutControlCharacters(text);
        bool trimmed = TrimLog(text.Length);

        int selectionStart;
        int selectionEnd;
        SendMessageW(_log, EM_GETSEL, (nint)(&selectionStart), (nint)(&selectionEnd));
        int length = GetWindowTextLengthW(_log);
        bool follow = trimmed || (selectionStart == selectionEnd && selectionEnd == length && ScrolledToEnd());
        int firstVisible = (int)SendMessageW(_log, EM_GETFIRSTVISIBLELINE, 0, 0);

        SendMessageW(_log, EM_SETSEL, length, length);
        fixed (char* p = text)
        {
            SendMessageW(_log, EM_REPLACESEL, 0, (nint)p);
        }

        if (follow)
        {
            SendMessageW(_log, EM_SCROLLCARET, 0, 0);
            return;
        }

        SendMessageW(_log, EM_SETSEL, selectionStart, selectionEnd);
        int nowVisible = (int)SendMessageW(_log, EM_GETFIRSTVISIBLELINE, 0, 0);
        if (nowVisible != firstVisible)
        {
            SendMessageW(_log, EM_LINESCROLL, 0, firstVisible - nowVisible);
        }
    }

    /// <summary>Drops the oldest half of the log, at a line boundary, when it would grow past the limit.</summary>
    private bool TrimLog(int adding)
    {
        int length = GetWindowTextLengthW(_log);
        if (length + adding <= MaxLogChars)
        {
            return false;
        }

        int cut = Math.Min(length, length + adding - MaxLogChars / 2);
        int line = (int)SendMessageW(_log, EM_LINEFROMCHAR, cut, 0);
        int lineStart = (int)SendMessageW(_log, EM_LINEINDEX, line + 1, 0);
        int end = lineStart > 0 ? lineStart : length;
        string note = Strings.RunnerLogTrimmed() + "\r\n";
        SendMessageW(_log, EM_SETSEL, 0, end);
        fixed (char* p = note)
        {
            SendMessageW(_log, EM_REPLACESEL, 0, (nint)p);
        }

        return true;
    }

    private bool ScrolledToEnd()
    {
        var info = new SCROLLINFO { cbSize = (uint)sizeof(SCROLLINFO), fMask = SIF_RANGE | SIF_PAGE | SIF_POS };
        return !GetScrollInfo(_log, SB_VERT, &info) || info.nPage == 0 || info.nPos + (int)info.nPage >= info.nMax;
    }

    /// <summary>
    /// EM_REPLACESEL stops at the first NUL, which UTF-16 output read as a code page is full
    /// of; other control characters only garble the line.
    /// </summary>
    private static string WithoutControlCharacters(string text)
    {
        if (!text.AsSpan().ContainsAnyInRange('\0', '\u001F'))
        {
            return text;
        }

        var clean = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            clean.Append(c < ' ' && c is not ('\t' or '\r' or '\n') ? '�' : c);
        }

        return clean.ToString();
    }

    private static string ToWindowsNewLines(string text) => text.Replace("\r\n", "\n").Replace("\n", "\r\n");

    private static int ShowFatal(Exception error)
    {
        // WM_DESTROY has already asked the loop to quit; a pending WM_QUIT would close the
        // message box before it is drawn.
        MSG pending;
        while (PeekMessageW(&pending, 0, WM_QUIT, WM_QUIT, PM_REMOVE))
        {
        }

        MessageBoxW(0, Strings.RunnerFatal(error.Message), ProductInfo.Name, MB_ICONERROR);
        return 3;
    }

    internal static uint DpiOf(nint hwnd)
    {
        uint dpi = hwnd != 0 ? GetDpiForWindow(hwnd) : 0;
        return dpi == 0 ? 96 : dpi;
    }

    internal static int Scale(int value, uint dpi) => (int)(value * (long)dpi / 96);
}
