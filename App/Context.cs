using System.Xml.Serialization;
using App.Configurations.Interfaces;
using App.Configurations.Realisation;
using App.System.Services;
using Platforms.Windows;
using Language = App.Configurations.Interfaces.Language;

namespace App;

public class Context
{
    public readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak"
    );
    
    public readonly string LogsDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak\\Logs"
    );
    
    public const string NameDataFile = "AppData.xml";
    public const string NameAuthorizationNetworkTokenFile = "AuthNetworkToken";

    public Guid CurrentSessionToken { get; } = Guid.NewGuid();


    public IAppConfig AppConfig { get; private set; }
    public IContextData ContextData { get; private set; }
    public System.Modules.Network Network { get; private set; }

    public void SetUp()
    {
        Logger.Write(Logger.Type.Info, "------- Starting SetUp context application ---------");
        
        if (!DataDirectoryInitialized())
            InitializeDataDirectory();
        
        // TODO: CLEAR THIS CODE
        
        var serializerContextData = new XmlSerializer(typeof(ContextData));
        using var readerContextData = new StreamReader(DataDirectory + "/" + NameDataFile);
        var context = (ContextData) serializerContextData.Deserialize(readerContextData);
        
        ContextData = context;
        
        App.System.Services.Language.Load(ContextData.LanguageSelected);
        
        var fileNetworkconfigXml = "NetworkConfig.xml";
        var fileAppConfigXml = "AppConfig.xml";
        
        try
        {
            var serializer = new XmlSerializer(typeof(NetworkConfig));
            using var reader = new StreamReader(fileNetworkconfigXml);
            var configInet = (NetworkConfig) serializer.Deserialize(reader);
            
            serializer = new XmlSerializer(typeof(AppConfig));
            using var readerAppConfig = new StreamReader(fileAppConfigXml);
            AppConfig = (AppConfig) serializer.Deserialize(readerAppConfig);
            
            Network = new System.Modules.Network(configInet);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to load network configuration", ex);
            throw new InvalidOperationException("Failed to load network configuration", ex);
        }
    }

    public bool DataDirectoryInitialized() => Directory.Exists(DataDirectory);

    private string _devicePrivateKey;
    public void InitializeDataDirectory()
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
    
    static Context() => Instance = new();
    public static Context Instance { get; private set; }
}