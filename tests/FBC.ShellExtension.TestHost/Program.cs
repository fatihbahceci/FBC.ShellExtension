using System.Runtime.InteropServices;
using FBC.ShellExtension.Core;
using static FBC.ShellExtension.TestHost.Native;

namespace FBC.ShellExtension.TestHost;

/// <summary>
/// Calls the shell extension as Explorer does, without registration and without Explorer:
/// a crash ends this program instead, and a rebuilt DLL needs no Explorer restart.
/// </summary>
internal static unsafe class Program
{
    private const uint FirstId = 1;
    private const uint LastId = 0x7FFF;

    private const string Usage = """
        Usage:
          FBC.ShellExtension.TestHost <dll> <path>... [--invoke <n>]
          FBC.ShellExtension.TestHost <dll> --background <folder> [--invoke <n>]

        Prints the menu the extension builds for the selection. --invoke runs the item shown
        as [n], which starts the runner next to the DLL. The config is the real one:
        %APPDATA%\FBCTools\config.json.
        """;

    [STAThread]
    private static int Main(string[] args)
    {
        if (!TryParse(args, out string dll, out List<string> paths, out string? background, out int? invoke))
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        try
        {
            Run(dll, paths, background, invoke);
            return 0;
        }
        catch (HResultException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void Run(string dll, List<string> paths, string? background, int? invoke)
    {
        Console.WriteLine($"TestHost {ProductInfo.InformationalVersion}, loading {Path.GetFullPath(dll)}");
        nint library = NativeLibrary.Load(Path.GetFullPath(dll));
        var getClassObject = (delegate* unmanaged<Guid*, Guid*, nint*, int>)NativeLibrary.GetExport(library, "DllGetClassObject");

        Guid clsid = ProductInfo.ContextMenuClsid;
        Guid iid = IID_IClassFactory;
        nint factory;
        Check(getClassObject(&clsid, &iid, &factory), "DllGetClassObject");

        nint init;
        iid = IID_IShellExtInit;
        var createInstance = (delegate* unmanaged<nint, nint, Guid*, nint*, int>)Slot(factory, 3);
        Check(createInstance(factory, 0, &iid, &init), "IClassFactory.CreateInstance");
        Release(factory);

        using Selection selection = background is not null
            ? Selection.ForBackground(background)
            : Selection.ForItems(paths);

        var initialize = (delegate* unmanaged<nint, nint, nint, nint, int>)Slot(init, 3);
        Check(initialize(init, selection.FolderPidl, selection.DataObject, 0), "IShellExtInit.Initialize");

        Check(Marshal.QueryInterface(init, IID_IContextMenu, out nint contextMenu), "QueryInterface(IContextMenu)");
        Release(init);

        nint menu = CreatePopupMenu();
        try
        {
            var queryContextMenu = (delegate* unmanaged<nint, nint, uint, uint, uint, uint, int>)Slot(contextMenu, 3);
            int hr = queryContextMenu(contextMenu, menu, 0, FirstId, LastId, CMF_NORMAL);
            Check(hr, "IContextMenu.QueryContextMenu");
            Console.WriteLine($"QueryContextMenu: {hr & 0xFFFF} command id(s)");
            PrintMenu(menu, 0);

            if (invoke is int offset)
            {
                var info = new CMINVOKECOMMANDINFO { cbSize = (uint)sizeof(CMINVOKECOMMANDINFO), lpVerb = offset, nShow = SW_SHOWNORMAL };
                var invokeCommand = (delegate* unmanaged<nint, CMINVOKECOMMANDINFO*, int>)Slot(contextMenu, 4);
                Check(invokeCommand(contextMenu, &info), $"IContextMenu.InvokeCommand({offset})");
                Console.WriteLine($"InvokeCommand({offset}): started the runner");
            }
        }
        finally
        {
            DestroyMenu(menu);
            Release(contextMenu);
        }
    }

    private static void PrintMenu(nint menu, int depth)
    {
        string indent = new(' ', depth * 2);
        int count = GetMenuItemCount(menu);
        for (int i = 0; i < count; i++)
        {
            // A submenu's state carries its item count in the high byte, so it is checked first.
            nint submenu = GetSubMenu(menu, i);
            if (submenu != 0)
            {
                Console.WriteLine($"{indent}{GetText(menu, i)} >");
                PrintMenu(submenu, depth + 1);
                continue;
            }

            uint state = GetMenuState(menu, (uint)i, MF_BYPOSITION);
            if ((state & MF_SEPARATOR) != 0)
            {
                Console.WriteLine($"{indent}----");
                continue;
            }

            string disabled = (state & MF_GRAYED) != 0 ? " (disabled)" : "";
            Console.WriteLine($"{indent}[{GetMenuItemID(menu, i) - FirstId}] {GetText(menu, i)}{disabled}");
        }
    }

    private static string GetText(nint menu, int position)
    {
        char* buffer = stackalloc char[512];
        int length = GetMenuString(menu, (uint)position, buffer, 512, MF_BYPOSITION);
        return new string(buffer, 0, Math.Max(length, 0));
    }

    internal static void Check(int hr, string what)
    {
        if (hr < 0)
        {
            throw new HResultException($"{what} failed: 0x{hr:X8}");
        }
    }

    private static bool TryParse(string[] args, out string dll, out List<string> paths, out string? background, out int? invoke)
    {
        dll = "";
        paths = [];
        background = null;
        invoke = null;
        if (args.Length < 2)
        {
            return false;
        }

        dll = args[0];
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--background" when i + 1 < args.Length:
                    background = args[++i];
                    break;
                case "--invoke" when i + 1 < args.Length && int.TryParse(args[i + 1], out int n):
                    invoke = n;
                    i++;
                    break;
                default:
                    if (args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    paths.Add(args[i]);
                    break;
            }
        }

        return (background is null) != (paths.Count == 0);
    }
}
