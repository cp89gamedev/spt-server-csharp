using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Diagnostics;

namespace UnitTests.Mock;

/// <summary>
/// Mock of ImporterUtil for unit tests.
/// Simply inherits from ImporterUtil and uses injected MockFileUtil and MockJsonUtil.
/// Since the base class uses the injected dependencies, this will automatically use the mocks.
/// </summary>
[Injectable(TypeOverride = typeof(ImporterUtil))]
public class MockImporterUtil(ISptLogger<ImporterUtil> logger, FileUtil fileUtil, JsonUtil jsonUtil)
    : ImporterUtil(logger, fileUtil, jsonUtil)
{
    // Override LoadRecursiveAsync to use the base implementation but log for debugging
    protected override async Task<object> LoadRecursiveAsync(
        string filePath,
        Type loadedType,
        Func<string, Task>? onReadCallback = null,
        Func<string, object, Task>? onObjectDeserialized = null
    )
    {
        Debug.WriteLine($"[MockImporterUtil] LoadRecursiveAsync called for path: {filePath}, type: {loadedType.Name}");
        
        try
        {
            // Call base implementation which will use our injected MockFileUtil and MockJsonUtil
            var result = await base.LoadRecursiveAsync(filePath, loadedType, onReadCallback, onObjectDeserialized);
            Debug.WriteLine($"[MockImporterUtil] Successfully loaded data from: {filePath}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MockImporterUtil] Error loading from {filePath}: {ex.Message}");
            throw;
        }
    }
}
