using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace UnitTests.Mocks;

/// <summary>
/// Mock FileUtil for unit tests. No external dependencies, does not inherit from any class.
/// - Mocks FileUtil's public API using System.IO
/// - Also provides recursive JSON loading helpers used by ImporterUtil mocks
/// </summary>
[Injectable(TypeOverride = typeof(FileUtil))]
public class MockFileUtil
{
    private const string ModBasePath = "user/mods/";

    // ===== FileUtil public API mocks =====

    public List<string> GetFiles(string path, bool recursive = false, string searchPattern = "*")
    {
        var files = new List<string>(Directory.GetFiles(path, searchPattern));
        if (recursive)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                files.AddRange(GetFiles(dir, recursive, searchPattern));
            }
        }
        return files;
    }

    public string[] GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    public string GetFileExtension(string path)
    {
        return Path.GetExtension(path).Replace(".", "");
    }

    public string GetFileNameAndExtension(string path)
    {
        return Path.GetFileName(path);
    }

    public string StripExtension(string path, bool keepPath = false)
    {
        if (keepPath)
        {
            return path.StartsWith(".") ? path.Split('.')[1] : path.Split('.').First();
        }
        return Path.GetFileNameWithoutExtension(path);
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);

    public bool FileExists(string path) => File.Exists(path);

    public string ReadFile(string path) => File.ReadAllText(path);

    public async Task<string> ReadFileAsync(string path) => await File.ReadAllTextAsync(path);

    public async Task<byte[]> ReadFileAsBytesAsync(string path) => await File.ReadAllBytesAsync(path);

    public void WriteFile(string filePath, string fileContent)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, fileContent);
    }

    public void WriteFile(string filePath, byte[] fileContent)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(filePath, fileContent);
    }

    public async Task WriteFileAsync(string filePath, string fileContent)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(filePath, fileContent);
    }

    public async Task WriteFileAsync(string filePath, byte[] fileContent)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(filePath, fileContent);
    }

    public bool DeleteFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;
        File.Delete(filePath);
        return true;
    }

    public bool CopyFile(string copyFromPath, string destinationFilePath, bool overwrite = false)
    {
        if (!File.Exists(copyFromPath))
            return false;
        var dir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.Copy(copyFromPath, destinationFilePath, overwrite);
        return true;
    }

    public void DeleteDirectory(string directory, bool deleteContent = false)
    {
        Directory.Delete(directory, deleteContent);
    }

    public string GetModPath(string modName) => Path.Combine(ModBasePath, modName);

    // ===== Recursive JSON loading helpers (migrated from mock importer) =====

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<T> LoadRecursiveAsync<T>(
        string filePath,
        Func<string, Task>? onReadCallback = null,
        Func<string, object, Task>? onObjectDeserialized = null
    )
    {
        var result = Activator.CreateInstance<T>();
        await LoadRecursiveAsync(filePath, typeof(T), result!, onReadCallback, onObjectDeserialized, null);
        return result!;
    }

    // Overload that allows a custom deserializer (e.g., MockJsonUtil)
    public async Task<T> LoadRecursiveAsync<T>(
        string filePath,
        Func<string, Type, Task<object?>> customDeserializer,
        Func<string, Task>? onReadCallback = null,
        Func<string, object, Task>? onObjectDeserialized = null
    )
    {
        var result = Activator.CreateInstance<T>();
        await LoadRecursiveAsync(filePath, typeof(T), result!, onReadCallback, onObjectDeserialized, customDeserializer);
        return result!;
    }

    public MethodInfo GetSetMethod(string name, Type type, out Type propertyType, out bool isDictionary)
    {
        isDictionary = false;
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
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            isDictionary = true;
            var keyType = propertyType.GetGenericArguments()[0];
            var valueType = propertyType.GetGenericArguments()[1];
            return propertyType.GetMethod("Add", new[] { keyType, valueType })!;
        }

        return prop.SetMethod!;
    }

    private async Task LoadRecursiveAsync(
        string path,
        Type targetType,
        object target,
        Func<string, Task>? onReadCallback,
        Func<string, object, Task>? onObjectDeserialized,
        Func<string, Type, Task<object?>>? customDeserializer
    )
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
        {
            if (onReadCallback != null)
                await onReadCallback(file);
            await ProcessFileAsync(file, targetType, target, onObjectDeserialized, customDeserializer);
        }

        foreach (var directory in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
        {
            await ProcessDirectoryAsync(directory, targetType, target, onReadCallback, onObjectDeserialized, customDeserializer);
        }
    }

    private async Task ProcessFileAsync(string file, Type targetType, object target, Func<string, object, Task>? onObjectDeserialized, Func<string, Type, Task<object?>>? customDeserializer)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        var setMethod = GetSetMethod(fileName, targetType, out var propertyType, out var isDictionary);

        var deserialized = customDeserializer != null ? await customDeserializer(file, propertyType) : await DeserializeFileAsync(file, propertyType);
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
        Func<string, object, Task>? onObjectDeserialized,
        Func<string, Type, Task<object?>>? customDeserializer
    )
    {
        var directoryName = Path.GetFileName(directory);
        var setMethod = GetSetMethod(directoryName, targetType, out var propertyType, out var isDictionary);

        var nested = Activator.CreateInstance(propertyType)!;
        await LoadRecursiveAsync(directory + Path.DirectorySeparatorChar, propertyType, nested, onReadCallback, onObjectDeserialized, customDeserializer);

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
        await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return await JsonSerializer.DeserializeAsync(fs, propertyType, JsonOptions);
    }

    private static string NormalizeName(string raw)
    {
        return raw.Replace("_", "").Replace("-", "").Trim().ToLowerInvariant();
    }
}
