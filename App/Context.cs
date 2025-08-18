using System.Xml.Serialization;
using App.Configurations.Realisation;
using App.System.Services;

namespace App;

public class Context
{
    public AppConfig AppConfig { get; private set; }
    
    public System.Modules.Network Network { get; private set; }
    
    public Authorization Authorization { get; private set; }

    public void SetUp()
    {
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
            throw new InvalidOperationException("Failed to load network configuration", ex);
        }
    }
    
    static Context() => Instance = new();
    public static Context Instance { get; private set; }
}