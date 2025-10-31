using System.Xml.Serialization;
using App.Configurations.Data;
using App.Configurations.Roots;
using App.System.Calls.Application.Facade;
using App.System.Services;
using Platforms;
using Platforms.Interfaces;
using Platforms.Windows;

namespace App;

public static class Context
{
    public static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak"
    );
    
    public static readonly string LogsDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak\\Logs"
    );
    
    public static readonly string DownloadDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak\\Downloads"
    );
    
    public const string NameDataFile = "AppData.xml";
    public const string NameUserSettingsFile = "CommunicationSettings.xml";
    public const string NameAuthorizationNetworkTokenFile = "AuthNetworkToken";

    public static Guid CurrentSessionToken { get; } = Guid.NewGuid();
    public static Platforms.Platforms CurrentPlatform { get; private set; } = Platforms.Platforms.Windows;

    public static AppConfig AppConfig { get; private set; }
    public static ContextData ContextData { get; private set; }
    public static UserSettings UserSettings { get; private set; }
    public static System.Modules.Network Network { get; private set; }

    public static CallFacade CallFacade { get; private set; }

    public static void SetUp()
    {
        Logger.Write(Logger.Type.Info, "------- Starting SetUp context application ---------");
        
        
        if (!DataDirectoryInitialized())
            InitializeDataDirectory();
        
        ContextData = new ContextData();
        ContextData.LoadOrDefault();
        
        PlatformServiceFactory.Register( new List<ISecureStorage>
        {
            new Platforms.Windows.SecureStorage(),
            new Platforms.MacOS.SecureStorage()
        });
            
        SecureStorage = PlatformServiceFactory.GetService<ISecureStorage>(CurrentPlatform);
        
        System.Services.Language.Load(ContextData.LanguageSelected);

        try
        {
            _devicePrivateKey = SecureStorage.Load("device_private_key");
            if (string.IsNullOrEmpty(_devicePrivateKey))
            {
                (string publicKey, string privateKey) = DeviceCrypto.GenerateKeyPair();
                _devicePrivateKey = privateKey;
                SecureStorage.Save("device_private_key", _devicePrivateKey);
                SecureStorage.Save("device_public_key", publicKey);
            }
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to ensure device keys in SecureStorage", ex);
        }

        var netConfig = new NetworkConfig();
        netConfig.LoadOrDefault();
        Network = new System.Modules.Network(netConfig);

        AppConfig = new AppConfig();
        AppConfig.LoadOrDefault();

        UserSettings = new UserSettings();
        UserSettings.LoadOrDefault();

        // DEMO TEST
        Network.OnServerConnected += () =>
        {
            CallFacade = new CallFacade(Network.Config, Network.WebSocketClient);
        };
    }

    public static bool DataDirectoryInitialized() => Directory.Exists(DataDirectory);

    private static string _devicePrivateKey;
    
    
    public static ISecureStorage SecureStorage { get; private set; }
    public static void InitializeDataDirectory()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(LogsDataDirectory);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to initialize data directory", ex);
            throw new InvalidOperationException("Failed to initialize data directory", ex);
        }
    }
}