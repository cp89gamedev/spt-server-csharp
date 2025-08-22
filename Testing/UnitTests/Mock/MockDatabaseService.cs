using System.Diagnostics;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Server.Core.Services;
using UnitTests.Mock;
using UnitTests.Mocks;
using Hideout = SPTarkov.Server.Core.Models.Spt.Hideout.Hideout;
using Locations = SPTarkov.Server.Core.Models.Spt.Server.Locations;

namespace UnitTests.Mock;

/// <summary>
/// Mock implementation of DatabaseService for unit tests.
/// Backed by an in-memory DatabaseTables instance that tests can configure.
/// </summary>
[Injectable(TypeOverride = typeof(DatabaseService))]
public class MockDatabaseService
{
    private DatabaseTables? _tables;
    private bool _isDataValid = true;
    private readonly MockImporterUtil _importerUtil;

    /// <summary>
    /// Optionally initialize with prebuilt tables.
    /// </summary>
    public MockDatabaseService(DatabaseTables? tables = null)
    {
        _tables = tables;
    }

    /// <summary>
    /// Initialize mock by auto-loading DatabaseTables from JSON fixture files.
    /// Looks under Testing/UnitTests/TestAssets/database.
    /// </summary>
    public MockDatabaseService(MockFileUtil fileUtil, MockJsonUtil jsonUtil)
    {
        const string basePath = "Testing/UnitTests/TestAssets/database/";
        try
        {
            _tables = fileUtil
                .LoadRecursiveAsync<DatabaseTables>(basePath, (file, type) => Task.FromResult(jsonUtil.DeserializeFromFile(file, type))!)
                .GetAwaiter()
                .GetResult();
            _isDataValid = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MockDatabaseService] Failed to load test database from '{basePath}': {ex.Message}");
            _tables = null;
            _isDataValid = false;
        }
    }

    /// <summary>
    /// Replace the in-memory tables used by this mock.
    /// </summary>
    public void SetTables(DatabaseTables tables)
    {
        _tables = tables;
    }

    /// <summary>
    /// Get the current in-memory tables.
    /// </summary>
    public DatabaseTables GetTables()
    {
        EnsureTables();
        return _tables!;
    }

    public Bots GetBots()
    {
        return GetTables().Bots;
    }

    public Globals GetGlobals()
    {
        return GetTables().Globals;
    }

    public Hideout GetHideout()
    {
        return GetTables().Hideout;
    }

    public LocaleBase GetLocales()
    {
        return GetTables().Locales;
    }

    public Locations GetLocations()
    {
        return GetTables().Locations;
    }

    public Location? GetLocation(string locationId)
    {
        var desiredLocation = GetLocations().GetByJsonProp<Location>(locationId.ToLowerInvariant());
        return desiredLocation;
    }

    public Match GetMatch()
    {
        return GetTables().Match;
    }

    public ServerBase GetServer()
    {
        return GetTables().Server;
    }

    public SettingsBase GetSettings()
    {
        return GetTables().Settings;
    }

    public Templates GetTemplates()
    {
        return GetTables().Templates;
    }

    public List<Achievement> GetAchievements()
    {
        return GetTables().Templates.Achievements;
    }

    public List<Achievement> GetCustomAchievements()
    {
        return GetTables().Templates.CustomAchievements;
    }

    public Dictionary<MongoId, CustomizationItem> GetCustomization()
    {
        return GetTables().Templates.Customization;
    }

    public HandbookBase GetHandbook()
    {
        return GetTables().Templates.Handbook;
    }

    public Dictionary<MongoId, TemplateItem> GetItems()
    {
        return GetTables().Templates.Items;
    }

    public Dictionary<MongoId, double> GetPrices()
    {
        return GetTables().Templates.Prices;
    }

    public Dictionary<string, ProfileSides> GetProfileTemplates()
    {
        return GetTables().Templates.Profiles;
    }

    public Dictionary<MongoId, Quest> GetQuests()
    {
        return GetTables().Templates.Quests;
    }

    public Dictionary<MongoId, Trader> GetTraders()
    {
        return GetTables().Traders;
    }

    public Trader? GetTrader(MongoId traderId)
    {
        return GetTraders().TryGetValue(traderId, out var trader) ? trader : null;
    }

    public LocationServices GetLocationServices()
    {
        var templates = GetTables().Templates;
        if (templates?.LocationServices == null)
        {
            throw new Exception("assets/database/locationServices.json is missing in mock tables");
        }

        return templates.LocationServices;
    }

    /// <summary>
    /// Validate that key-based tables contain valid Mongo IDs.
    /// Simplified from the real service: writes debug output but does not log via logger.
    /// </summary>
    public void ValidateDatabase()
    {
        var start = Stopwatch.StartNew();

        _isDataValid =
            ValidateTable(GetQuests(), "quest")
            && ValidateTable(GetTraders(), "trader")
            && ValidateTable(GetItems(), "item")
            && ValidateTable(GetCustomization(), "customization");

        start.Stop();
        Debug.WriteLine($"[MockDatabaseService] ID validation took: {start.ElapsedMilliseconds}ms. Valid={_isDataValid}");
    }

    public bool IsDatabaseValid()
    {
        return _isDataValid;
    }

    private void EnsureTables()
    {
        if (_tables == null)
        {
            throw new InvalidOperationException(
                "MockDatabaseService has no tables set. Call SetTables() with a configured DatabaseTables instance before use."
            );
        }
    }

    private bool ValidateTable<T>(Dictionary<string, T> table, string tableType)
    {
        foreach (var kvp in table)
        {
            if (!kvp.Key.IsValidMongoId())
            {
                Debug.WriteLine($"[MockDatabaseService] Invalid {tableType} ID: '{kvp.Key}'");
                return false;
            }
        }

        return true;
    }

    private bool ValidateTable<T>(Dictionary<MongoId, T> table, string tableType)
    {
        foreach (var kvp in table)
        {
            if (!kvp.Key.IsValidMongoId())
            {
                Debug.WriteLine($"[MockDatabaseService] Invalid {tableType} ID: '{kvp.Key}'");
                return false;
            }
        }

        return true;
    }
}
