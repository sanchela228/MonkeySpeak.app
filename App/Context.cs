using System.Xml.Serialization;
using App.Configurations.Interfaces;
using App.Configurations.Realisation;
using App.System.Calls.Application.Facade;
using App.System.Services;
using Platforms;
using Platforms.Interfaces;
using Platforms.Windows;
using Language = App.Configurations.Interfaces.Language;

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
    public const string NameCommunicationSettingsFile = "CommunicationSettings.xml";
    public const string NameAuthorizationNetworkTokenFile = "AuthNetworkToken";

    public static Guid CurrentSessionToken { get; } = Guid.NewGuid();
    public static Platforms.Platforms CurrentPlatform { get; private set; } = Platforms.Platforms.Windows;

    public static IAppConfig AppConfig { get; private set; }
    public static IContextData ContextData { get; private set; }
    public static ICommunicationSettings CommunicationSettings { get; private set; }
    public static System.Modules.Network Network { get; private set; }

    public static CallFacade CallFacade { get; private set; }

    public static void SetUp()
    {
        Logger.Write(Logger.Type.Info, "------- Starting SetUp context application ---------");
        
       
        if (!DataDirectoryInitialized())
            InitializeDataDirectory();
        
        // TODO: CLEAR THIS CODE
        
        var serializerContextData = new XmlSerializer(typeof(ContextData));
        using var readerContextData = new StreamReader(DataDirectory + "/" + NameDataFile);
        var context = (ContextData) serializerContextData.Deserialize(readerContextData);
        
        ContextData = context;
        
        PlatformServiceFactory.Register( new List<ISecureStorage>
        {
            new Platforms.Windows.SecureStorage(),
            new Platforms.MacOS.SecureStorage()
        });
            
        SecureStorage = PlatformServiceFactory.GetService<ISecureStorage>(CurrentPlatform);
        
        App.System.Services.Language.Load(ContextData.LanguageSelected);
        
        const string fileNetworkConfigXml = "NetworkConfig.xml";
        const string fileAppConfigXml = "AppConfig.xml";
        
        try
        {
            var serializer = new XmlSerializer(typeof(NetworkConfig));
            using var reader = new StreamReader(fileNetworkConfigXml);
            var configInet = (NetworkConfig) serializer.Deserialize(reader);
            
            serializer = new XmlSerializer(typeof(AppConfig));
            using var readerAppConfig = new StreamReader(fileAppConfigXml);
            AppConfig = (AppConfig) serializer.Deserialize(readerAppConfig);
            
            Network = new System.Modules.Network(configInet);
            CommunicationSettings = new CommunicationSettings();

            // DEMO TEST
            Network.OnServerConnected += () =>
            {
                CallFacade = new CallFacade(Network.Config, Network.WebSocketClient);
            };
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to load network configuration", ex);
            throw new InvalidOperationException("Failed to load network configuration", ex);
        }
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
            
            var context = new ContextData()
            {
                ApplicationId = Guid.NewGuid(),
                MachineId = ComputerIdentity.GetMacAddress(),
                LanguageSelected = Language.English
            };
            
            context.SaveContext();
            ContextData = context;
            
            // TODO: ADD ANY PLATFORM ENTRY METHOD
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
            Logger.Write(Logger.Type.Error, "Failed to initialize data directory", ex);
            throw new InvalidOperationException("Failed to initialize data directory", ex);
        }
    }
}