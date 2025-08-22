using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace UnitTests.Mock;

[Injectable(TypeOverride = typeof(JsonUtil))]
public class MockJsonUtil
{
    public static JsonSerializerOptions? JsonSerializerOptionsIndented { get; private set; } = new JsonSerializerOptions
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static JsonSerializerOptions? JsonSerializerOptionsNoIndent { get; private set; } = new JsonSerializerOptions
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Mirror ctor but without external registrators; keep defaults minimal
    public MockJsonUtil() { }

    public T? Deserialize<T>(string? json)
    {
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, JsonSerializerOptionsNoIndent);
    }

    public object? Deserialize(string? json, Type type)
    {
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize(json, type, JsonSerializerOptionsNoIndent);
    }

    public T? DeserializeFromFile<T>(string file)
    {
        if (!File.Exists(file)) return default;
        using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize<T>(fs, JsonSerializerOptionsNoIndent);
    }

    public async Task<T?> DeserializeFromFileAsync<T>(string file)
    {
        if (!File.Exists(file)) return default;
        await using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return await JsonSerializer.DeserializeAsync<T>(fs, JsonSerializerOptionsNoIndent);
    }

    public object? DeserializeFromFile(string file, Type type)
    {
        if (!File.Exists(file)) return default;
        using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize(fs, type, JsonSerializerOptionsNoIndent);
    }

    public async Task<object?> DeserializeFromFileAsync(string file, Type type)
    {
        if (!File.Exists(file)) return default;
        await using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return await JsonSerializer.DeserializeAsync(fs, type, JsonSerializerOptionsNoIndent);
    }

    public object? DeserializeFromFileStream(FileStream fs, Type type)
    {
        return JsonSerializer.Deserialize(fs, type, JsonSerializerOptionsNoIndent);
    }

    public async Task<object?> DeserializeFromFileStreamAsync(FileStream fs, Type type)
    {
        return await JsonSerializer.DeserializeAsync(fs, type, JsonSerializerOptionsNoIndent);
    }

    public async Task<T?> DeserializeFromMemoryStreamAsync<T>(MemoryStream ms)
    {
        return await JsonSerializer.DeserializeAsync<T>(ms, JsonSerializerOptionsNoIndent);
    }

    public string? Serialize<T>(T? obj, bool indented = false)
    {
        if (obj == null) return null;
        return JsonSerializer.Serialize(obj, indented ? JsonSerializerOptionsIndented : JsonSerializerOptionsNoIndent);
    }

    public string? Serialize(object? obj, Type type, bool indented = false)
    {
        if (obj == null) return null;
        return JsonSerializer.Serialize(obj, type, indented ? JsonSerializerOptionsIndented : JsonSerializerOptionsNoIndent);
    }
}
