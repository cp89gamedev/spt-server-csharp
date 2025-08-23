using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace UnitTests.Mock;

/// <summary>
/// Mock implementation of DatabaseImporter for unit tests.
/// Prevents loading of production database during test initialization.
/// The MockDatabaseService will handle loading test data instead.
/// </summary>
[Injectable(TypeOverride = typeof(DatabaseImporter))]
public class MockDatabaseImporter : IOnLoad
{
    /// <summary>
    /// No-op implementation that doesn't load any database.
    /// MockDatabaseService will handle loading test data.
    /// </summary>
    public Task OnLoad()
    {
        // Do nothing - MockDatabaseService will load test data
        return Task.CompletedTask;
    }
}
