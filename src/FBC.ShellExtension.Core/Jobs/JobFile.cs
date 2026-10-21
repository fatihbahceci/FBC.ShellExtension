using System.Text.Json;
using System.Text.Json.Serialization;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Jobs;

/// <summary>
/// The job travels in a file because a command line holds at most 32,767 characters and a
/// large selection does not fit (ADR-0005). The runner deletes the file as soon as it has read it.
/// </summary>
public static class JobFile
{
    public const string Argument = "--job";

    public static string FolderPath => Path.Combine(Path.GetTempPath(), ProductInfo.Name, "jobs");

    /// <returns>The path of the new file.</returns>
    public static string Write(RunJob job) => Write(job, FolderPath);

    public static string Write(RunJob job, string folder)
    {
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(job, JobJsonContext.Default.RunJob));
        return path;
    }

    /// <summary>Reads the job and deletes the file, whether or not it could be read.</summary>
    /// <exception cref="JobFileException">The file is missing, unreadable or from another version.</exception>
    public static RunJob ReadAndDelete(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new JobFileException(Strings.JobInvalid(ex.Message), ex);
        }
        finally
        {
            TryDelete(path);
        }

        RunJob? job;
        try
        {
            job = JsonSerializer.Deserialize(json, JobJsonContext.Default.RunJob);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new JobFileException(Strings.JobInvalid(ex.Message), ex);
        }

        if (job is null)
        {
            throw new JobFileException(Strings.JobInvalid(""), null);
        }

        if (job.Version != RunJob.CurrentVersion)
        {
            throw new JobFileException(Strings.JobUnsupportedVersion(job.Version, RunJob.CurrentVersion), null);
        }

        return job;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A leftover file in %TEMP% is harmless; failing the job over it is not.
        }
    }
}

public sealed class JobFileException(string message, Exception? innerException)
    : Exception(message, innerException);

// Written and read by the same build, so the format is strict and compact.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(RunJob))]
internal sealed partial class JobJsonContext : JsonSerializerContext
{
}
