using System.Text.Json;
using System.Text.Json.Serialization;

namespace FBC.ShellExtension.Core.Configuration;

// The config is edited by hand as well as by the editor: comments and trailing commas are
// accepted, and an unknown property is an error so that a misspelt key is not silently ignored.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    AllowOutOfOrderMetadataProperties = true,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(MenuConfig))]
internal sealed partial class ConfigJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Enum values are written as camelCase names; numbers are rejected, and so are comma-separated
/// lists: the base converter would OR them into another value ("copyName, copyUncPath" reads as
/// copyContent). Public because the enums carry it as an attribute, and a source-generated
/// context in another assembly has to be able to instantiate it.
/// </summary>
public sealed class CamelCaseEnumConverter<TEnum> : JsonConverterFactory
    where TEnum : struct, Enum
{
    private readonly JsonStringEnumConverter<TEnum> _names = new(JsonNamingPolicy.CamelCase, allowIntegerValues: false);

    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TEnum);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        new SingleValue((JsonConverter<TEnum>)_names.CreateConverter(typeToConvert, options)!);

    private sealed class SingleValue(JsonConverter<TEnum> inner) : JsonConverter<TEnum>
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && reader.GetString()!.Contains(','))
            {
                // No message: the serializer then reports it like any other bad value, with its path.
                throw new JsonException();
            }

            return inner.Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
            inner.Write(writer, value, options);
    }
}
