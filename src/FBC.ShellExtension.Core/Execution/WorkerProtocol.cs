using System.Text.Json;
using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Commands;
using FBC.ShellExtension.Core.Configuration;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Execution;

/// <summary>
/// Messages between the runner and its elevated worker, one JSON object per line over a named
/// pipe (ADR-0006). The runner sends the job and, if the user cancels, a cancel message; the
/// worker reports each process and ends with done.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WorkerJobMessage), "job")]
[JsonDerivedType(typeof(WorkerCancelMessage), "cancel")]
[JsonDerivedType(typeof(WorkerStartedMessage), "started")]
[JsonDerivedType(typeof(WorkerOutputMessage), "output")]
[JsonDerivedType(typeof(WorkerFinishedMessage), "finished")]
[JsonDerivedType(typeof(WorkerDoneMessage), "done")]
public abstract record WorkerMessage;

/// <summary>The plan is sent already expanded: under Windows 11 administrator protection the worker runs as another account with its own %TEMP% and environment.</summary>
public sealed record WorkerJobMessage(CommandDefinition Command, IReadOnlyList<PlannedProcess> Processes) : WorkerMessage;

public sealed record WorkerCancelMessage : WorkerMessage;

public sealed record WorkerStartedMessage(int Index) : WorkerMessage;

public sealed record WorkerOutputMessage(int Index, string Line) : WorkerMessage;

// The optional values are left out of the JSON when null, so they must not be required on the way back.
public sealed record WorkerFinishedMessage(int Index, ProcessOutcome Outcome, int? ExitCode = null, string? Message = null) : WorkerMessage;

public sealed record WorkerDoneMessage : WorkerMessage;

public static class WorkerProtocol
{
    public const string WorkerArgument = "--worker";

    /// <summary>One line: JSON escapes every newline inside strings.</summary>
    public static string Serialize(WorkerMessage message) =>
        JsonSerializer.Serialize(message, WorkerJsonContext.Default.WorkerMessage);

    /// <exception cref="WorkerProtocolException">The line is not a message.</exception>
    public static WorkerMessage Deserialize(string line)
    {
        try
        {
            return JsonSerializer.Deserialize(line, WorkerJsonContext.Default.WorkerMessage)
                ?? throw new WorkerProtocolException(Strings.WorkerProtocolError(line));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new WorkerProtocolException(Strings.WorkerProtocolError(ex.Message), ex);
        }
    }
}

public sealed class WorkerProtocolException(string message, Exception? innerException = null)
    : Exception(message, innerException);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(WorkerMessage))]
internal sealed partial class WorkerJsonContext : JsonSerializerContext
{
}
