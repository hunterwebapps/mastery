using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mastery.Application.Common;

/// <summary>
/// Provides GZip compression for JSON serialization.
/// Used for storing large snapshots in recommendation traces.
/// </summary>
public static class JsonCompressionHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes an object to compressed Base64 JSON.
    /// Format: gzip compressed UTF-8 JSON, then Base64 encoded.
    /// </summary>
    public static string SerializeCompressed<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return Convert.ToBase64String(output.ToArray());
    }

    /// <summary>
    /// Deserializes from compressed Base64 JSON.
    /// </summary>
    public static T? DeserializeCompressed<T>(string compressedBase64)
    {
        var compressed = Convert.FromBase64String(compressedBase64);

        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);

        var json = Encoding.UTF8.GetString(output.ToArray());
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// Serializes an object to regular (uncompressed) JSON.
    /// Used for smaller payloads like signal summaries.
    /// </summary>
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, JsonOptions);
    }
}
