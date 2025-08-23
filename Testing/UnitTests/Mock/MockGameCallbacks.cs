using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;

namespace UnitTests.Mock;

/// <summary>
/// Mock implementation of GameCallbacks for unit tests.
/// Prevents the production GameCallbacks.OnLoad from being called during tests.
/// </summary>
[Injectable(TypeOverride = typeof(GameCallbacks))]
public class MockGameCallbacks : IOnLoad
{
    /// <summary>
    /// No-op implementation that doesn't call gameController.Load().
    /// This prevents the PostDbLoadService from running before database is ready.
    /// </summary>
    public Task OnLoad()
    {
        // Do nothing - prevents gameController.Load() from being called
        // Tests can manually trigger gameController.Load() if needed
        return Task.CompletedTask;
    }
}
