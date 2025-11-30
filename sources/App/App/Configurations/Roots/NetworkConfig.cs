namespace App.Configurations.Roots;

public class NetworkConfig : XmlConfigBase<NetworkConfig>
{
    protected override string RootDirectory => AppContext.BaseDirectory;
    public string Domain { get; set; }
    public bool UseSSL { get; set; }
    public int Port { get; set; }
    public int MaxRetries { get; set; }
    
    public override string FileName => "NetworkConfig.xml";
    
    protected override void ApplyDefaults()
    {
        Domain = "localhost";
        UseSSL = false;
        Port = 8080;
        MaxRetries = 3;
    }
    
    protected override void CopyFrom(NetworkConfig other)
    {
        Domain = other.Domain;
        UseSSL = other.UseSSL;
        Port = other.Port;
        MaxRetries = other.MaxRetries;
    }
    
    public string DomainUrl()
    {
        string url = "";
        
        url += UseSSL ? "https://" : "http://";
        url += Domain + ":" + Port;
        
        return url; 
    }
}