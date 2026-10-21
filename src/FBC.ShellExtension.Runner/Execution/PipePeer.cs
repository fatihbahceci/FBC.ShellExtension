using System.IO.Pipes;
using static FBC.ShellExtension.Runner.Interop.Native;

namespace FBC.ShellExtension.Runner.Execution;

/// <summary>Which process is at the other end of the runner's pipe.</summary>
internal static class PipePeer
{
    public static bool ClientIs(NamedPipeServerStream server, int processId) =>
        GetNamedPipeClientProcessId(server.SafePipeHandle, out uint id) && id == (uint)processId;

    public static bool ServerIs(NamedPipeClientStream client, int processId) =>
        GetNamedPipeServerProcessId(client.SafePipeHandle, out uint id) && id == (uint)processId;
}
