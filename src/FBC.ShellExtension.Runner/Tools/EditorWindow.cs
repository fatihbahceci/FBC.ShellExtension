using System.ComponentModel;
using System.Diagnostics;
using FBC.ShellExtension.Core;
using FBC.ShellExtension.Core.Jobs;
using FBC.ShellExtension.Core.Resources;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Tools;

/// <summary>
/// The tools with a window of their own run in the editor's program, which has WinForms
/// (ADR-0028). The job goes on to it in a new job file, as it came here: the selection may not
/// fit a command line.
/// </summary>
internal static class EditorWindow
{
    public static int Run(RunJob job)
    {
        string title = Strings.RunnerWindowTitle(job.Label);
        string editor = Path.Combine(AppContext.BaseDirectory, ProductInfo.EditorFileName);
        if (!File.Exists(editor))
        {
            MessageBoxW(0, Strings.ToolNeedsEditor(ProductInfo.EditorFileName), title, MB_ICONERROR);
            return 1;
        }

        // The editor tells of a different shell extension build itself: it has the job's version.
        string jobPath = JobFile.Write(job);

        // Explorer started the runner from the click, so the runner may give the front away;
        // otherwise Windows' foreground rules may open the editor's window behind Explorer's.
        AllowSetForegroundWindow(ASFW_ANY);
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(editor)
            {
                UseShellExecute = false,
                ArgumentList = { JobFile.Argument, jobPath },
                WorkingDirectory = AppContext.BaseDirectory,
            });
            return 0;
        }
        catch (Exception ex) when (ex is Win32Exception or IOException)
        {
            File.Delete(jobPath);
            MessageBoxW(0, Strings.ToolEditorFailed(ex.Message), title, MB_ICONERROR);
            return 1;
        }
    }
}
