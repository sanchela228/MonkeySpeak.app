using System.Xml.Serialization;
using App.Configurations;
using App.Configurations.Realisation;
using App.System.Services;

namespace App;

public class Context
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak"
    );
    
    public readonly string LogsDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak/Logs"
    );
    
    private const string NameDataFile = "AppData.xml";
    private const string NameAuthorizationNetworkTokenFile = "AuthNetworkToken";
    
    private Guid CurrentSessionTokenKey = Guid.NewGuid();
    public Guid CurrentSessionToken => CurrentSessionTokenKey;
    
    
    public IAppConfig AppConfig { get; private set; }
    public IContextData ContextData { get; private set; }
    
    public System.Modules.Network Network { get; private set; }
    public Authorization Authorization { get; private set; }

    public void SetUp()
    {
        Logger.Write(Logger.Type.Info, "------- Starting SetUp context application ---------");
        
        if (!DataDirectoryInitialized())
            InitializeDataDirectory();
        
        // TODO: CLEAR THIS CODE
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
            Authorization = new Authorization(Network);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to load network configuration", ex);
            throw new InvalidOperationException("Failed to load network configuration", ex);
        }
    }

    public bool DataDirectoryInitialized() => Directory.Exists(DataDirectory);
    public void InitializeDataDirectory()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(LogsDataDirectory);

            var context = new ContextData()
            {
                ApplicationId = Guid.NewGuid(),
                MachineId = ComputerIdentity.GetMacAddress()
            };
            
            var serializer = new XmlSerializer(typeof(ContextData));
            using var writer = new StreamWriter(DataDirectory + "/" + NameDataFile);
            serializer.Serialize(writer, context);
            
            ContextData = context;
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