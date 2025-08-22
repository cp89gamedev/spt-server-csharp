using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace UnitTests.Mock;

/// <summary>
/// Mock of ImporterUtil for unit tests, with no external dependencies.
/// Recursively loads JSON fixtures from Testing/UnitTests/TestAssets.
/// Uses MockFileUtil for all file operations.
/// </summary>
[Injectable(TypeOverride = typeof(ImporterUtil))]
public class MockImporterUtil(ISptLogger<ImporterUtil> logger, FileUtil fileUtil, JsonUtil jsonUtil)
    : ImporterUtil(logger, fileUtil, jsonUtil)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    // Match ImporterUtil API (callbacks are optional and ignored by the mock unless provided)
    public async Task<T> LoadRecursiveAsync<T>(
        string filePath,
        Func<string, Task>? onReadCallback = null,
        Func<string, object, Task>? onObjectDeserialized = null
    )
    {
        var result = Activator.CreateInstance<T>();
        await LoadRecursiveAsync(filePath, typeof(T), result, onReadCallback, onObjectDeserialized);
        return result;
    }

    // Internal recursive loader
    private async Task LoadRecursiveAsync(
        string path,
        Type targetType,
        object target,
        Func<string, Task>? onReadCallback,
        Func<string, object, Task>? onObjectDeserialized
    )
    {
        if (!fileUtil.DirectoryExists(path))
        {
            return;
        }

        // Process files
        foreach (
            var file in fileUtil.GetFiles(path).Where(f => fileUtil.GetFileExtension(f).Equals("json", StringComparison.OrdinalIgnoreCase))
        )
        {
            if (onReadCallback != null)
                await onReadCallback(file);
            await ProcessFileAsync(file, targetType, target, onObjectDeserialized);
        }

        // Process directories
        foreach (var directory in fileUtil.GetDirectories(path))
        {
            await ProcessDirectoryAsync(directory, targetType, target, onReadCallback, onObjectDeserialized);
        }
    }

    private async Task ProcessFileAsync(string file, Type targetType, object target, Func<string, object, Task>? onObjectDeserialized)
    {
        var fileName = fileUtil.StripExtension(file);
        var setMethod = GetSetMethod(fileName, targetType, out var propertyType, out var isDictionary);

        var deserialized = await DeserializeFileAsync(file, propertyType);
        if (deserialized is null)
            return;

        if (onObjectDeserialized != null)
            await onObjectDeserialized(file, deserialized);

        if (isDictionary)
        {
            var key = fileName;
            setMethod.Invoke(target, new[] { key, deserialized });
        }
        else
        {
            setMethod.Invoke(target, new[] { deserialized });
        }
    }

    private async Task ProcessDirectoryAsync(
        string directory,
        Type targetType,
        object target,
        Func<string, Task>? onReadCallback,
        Func<string, object, Task>? onObjectDeserialized
    )
    {
        var directoryName = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Last();

        var setMethod = GetSetMethod(directoryName, targetType, out var propertyType, out var isDictionary);

        var nested = Activator.CreateInstance(propertyType)!;
        await LoadRecursiveAsync(directory + Path.DirectorySeparatorChar, propertyType, nested, onReadCallback, onObjectDeserialized);

        if (isDictionary)
        {
            setMethod.Invoke(target, new object[] { directoryName, nested });
        }
        else
        {
            setMethod.Invoke(target, new object[] { nested });
        }
    }

    private async Task<object?> DeserializeFileAsync(string file, Type propertyType)
    {
        var json = await fileUtil.ReadFileAsync(file);
        if (string.IsNullOrEmpty(json))
            return null;
        return JsonSerializer.Deserialize(json, propertyType, Options);
    }

    // Match ImporterUtil signature: public MethodInfo GetSetMethod(string propertyName, Type type, out Type propertyType, out bool isDictionary)
    public MethodInfo GetSetMethod(string name, Type type, out Type propertyType, out bool isDictionary)
    {
        isDictionary = false;

        // Handle case where the target itself is a dictionary (add entries)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            propertyType = type.GetGenericArguments()[1];
            var add = type.GetMethod("Add", new[] { type.GetGenericArguments()[0], propertyType });
            return add!;
        }

        var normalized = NormalizeName(name);

        var prop = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => string.Equals(NormalizeName(p.Name), normalized, StringComparison.OrdinalIgnoreCase));

        if (prop == null || prop.SetMethod == null)
        {
            throw new Exception($"Unable to find property '{name}' on type '{type.FullName}'");
        }

        propertyType = prop.PropertyType;

        // If property is a dictionary type, we'll populate via Add
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            isDictionary = true;
            var keyType = propertyType.GetGenericArguments()[0];
            var valueType = propertyType.GetGenericArguments()[1];
            return propertyType.GetMethod("Add", new[] { keyType, valueType })!;
        }

        return prop.SetMethod!;
    }

    private static string NormalizeName(string raw)
    {
        return raw.Replace("_", "").Replace("-", "").Trim().ToLowerInvariant();
    }
}
