using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SPTarkov.DI;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Logger.Handlers;
using UnitTests.Mock;

namespace UnitTests;

[TestFixture]
public class DI
{
    private static IServiceProvider _serviceProvider;

    private static DI? _instance;

    private DI()
    {
        ConfigureServices();
    }

    public static DI GetInstance()
    {
        return _instance ??= new DI();
    }

    private void ConfigureServices()
    {
        if (_serviceProvider != null)
        {
            return;
        }

        var services = new ServiceCollection();

        var diHandler = new DependencyInjectionHandler(services);

        diHandler.AddInjectableTypesFromTypeAssembly(typeof(App));
        diHandler.AddInjectableTypesFromTypeList(
            [
                typeof(MockLogger<>),
                typeof(MockImporterUtil), // Override ImporterUtil
                typeof(MockFileUtil), // Override FileUtil
                typeof(MockJsonUtil), // Override JsonUtil
                typeof(MockRandomUtil), // Override RandomUtil with deterministic behavior
                typeof(MockTimeUtil), // Override TimeUtil
                typeof(MockConfigServer), // Override ConfigServer
                typeof(MockDatabaseImporter), // Override DatabaseImporter to prevent loading production database
                typeof(MockGameCallbacks), // Override GameCallbacks to prevent gameController.Load() during test init
                typeof(MockDatabaseService), // Override DatabaseService
                typeof(MockRewardHelper), // Override RewardHelper
            ]
        );

        diHandler.InjectAll();

        services.AddSingleton<IReadOnlyList<SptMod>>(_ => []);

        _serviceProvider = services.BuildServiceProvider();

        foreach (var onLoad in _serviceProvider.GetServices<IOnLoad>())
        {
            // Only run OnLoad for our mock services, skip all others
            var typeName = onLoad.GetType().Name;
            System.Diagnostics.Debug.WriteLine($"[DI] Found IOnLoad service: {typeName}");
            if (typeName.StartsWith("Mock"))
            {
                System.Diagnostics.Debug.WriteLine($"[DI] Running OnLoad for: {typeName}");
                onLoad.OnLoad().Wait();
            }
        }
        
        // Explicitly ensure MockDatabaseService loads the test database
        var mockDbService = _serviceProvider.GetService<DatabaseService>();
        if (mockDbService is MockDatabaseService mockDb)
        {
            System.Diagnostics.Debug.WriteLine("[DI] Explicitly calling MockDatabaseService.OnLoad");
            mockDb.OnLoad().Wait();
        }
    }

    public T GetService<T>()
        where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}
