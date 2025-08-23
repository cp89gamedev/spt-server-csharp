using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace UnitTests.Mock;

/// <summary>
/// Mock implementation of DatabaseImporter for unit tests.
/// Prevents loading of production database during test initialization.
/// The MockDatabaseService will handle loading test data instead.
/// </summary>
[Injectable(TypeOverride = typeof(DatabaseImporter))]
public class MockDatabaseImporter(
    ISptLogger<DatabaseImporter> logger,
    FileUtil fileUtil,
    ServerLocalisationService serverLocalisationService,
    DatabaseServer databaseServer,
    ImageRouter imageRouter,
    ImporterUtil importerUtil,
    JsonUtil jsonUtil
) : DatabaseImporter(logger, fileUtil, serverLocalisationService, databaseServer, imageRouter, importerUtil, jsonUtil)
{
    /// <summary>
    /// No-op implementation that doesn't load any database.
    /// MockDatabaseService will handle loading test data.
    /// </summary>
    public async Task OnLoad()
    {
        const string basePath = "Testing/UnitTests/TestAssets/database/";
        await base.HydrateDatabase(basePath, false);
    }
}
