using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

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
    // The base class ImporterUtil already uses the injected fileUtil and jsonUtil,
    // so when we inject MockFileUtil and MockJsonUtil via DI, they will be used automatically.
    // No need to override anything unless we need specific test behavior.
}
