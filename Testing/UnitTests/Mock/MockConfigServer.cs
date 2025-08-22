using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace UnitTests.Mock;

[Injectable(TypeOverride = typeof(ConfigServer))]
public class MockConfigServer : ConfigServer
{
    private readonly Dictionary<string, object> _mockConfigs = new();
    private readonly FileUtil _file;
    private readonly JsonUtil _json;

    public MockConfigServer(ISptLogger<ConfigServer> logger, JsonUtil jsonUtil, FileUtil fileUtil)
        : base(logger, jsonUtil, fileUtil)
    {
        // Load a minimal set of configs from TestAssets based on real files under Libraries/SPTarkov.Server.Assets/SPT_Data/configs.
        // We only need a subset of top-level fields per file for testing.
        LoadMockConfigFromTestAsset<LocationConfig>("location.json", "spt-location");
        LoadMockConfigFromTestAsset<InRaidConfig>("inraid.json", "spt-inraid");
        LoadMockConfigFromTestAsset<TraderConfig>("trader.json", "spt-trader");
        LoadMockConfigFromTestAsset<RagfairConfig>("ragfair.json", "spt-ragfair");
        LoadMockConfigFromTestAsset<HideoutConfig>("hideout.json", "spt-hideout");
        LoadMockConfigFromTestAsset<PmcConfig>("pmc.json", "spt-pmc");
        LoadMockConfigFromTestAsset<LostOnDeathConfig>("lostondeath.json", "spt-lostondeath");
        _file = fileUtil;
        _json = jsonUtil;
    }

    private void LoadMockConfigFromTestAsset<T>(string fileName, string keyAlias)
        where T : BaseConfig
    {
        var testPath = Path.Combine("Testing", "UnitTests", "TestAssets", "configs", fileName);
        if (_file.FileExists(testPath))
        {
            var obj = _json.DeserializeFromFile<T>(testPath);
            if (obj != null)
            {
                // Store by both typeof(T).FullName and alias (e.g., "spt-location") for flexibility
                _mockConfigs[typeof(T).FullName!] = obj;
                _mockConfigs[keyAlias] = obj;
            }
        }
    }

    // Return a deserialized instance from TestAssets when present; otherwise, a default instance.
    public new T GetConfig<T>()
        where T : BaseConfig
    {
        var keyType = typeof(T).FullName!;
        if (_mockConfigs.TryGetValue(keyType, out var value))
        {
            return (T)value;
        }

        var created = Activator.CreateInstance<T>();
        return created;
    }

    // Same as above, but keyed by the provided string. Will return a default T if not present.
    public new T GetConfigByString<T>(string configType)
        where T : BaseConfig
    {
        if (_mockConfigs.TryGetValue(configType, out var value))
        {
            return (T)value;
        }

        var created = Activator.CreateInstance<T>();
        return created;
    }

    // Do not read from real SPT_Data in tests; rely on TestAssets.
    public new void Initialize()
    {
        // Intentionally blank; configs are loaded from TestAssets in the constructor.
    }
}
