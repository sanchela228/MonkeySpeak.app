namespace App.Configurations.Roots;

public class NetworkConfig : XmlConfigBase<NetworkConfig>
{
    protected override string RootDirectory => AppContext.BaseDirectory;
    public string Domain { get; set; }
    public bool UseSSL { get; set; }
    public int Port { get; set; }
    public string STUNServer { get; set; }
    public string TURNServer { get; set; }
    public int ConnectionTimeout { get; set; }
    public int MaxRetries { get; set; }
    
    public override string FileName => "NetworkConfig.xml";
    
    public override void ApplyDefaults()
    {
        Domain = "localhost";
        UseSSL = false;
        Port = 8080;
        STUNServer = string.Empty;
        TURNServer = string.Empty;
        ConnectionTimeout = 10000;
        MaxRetries = 3;
    }
    
    protected override void CopyFrom(NetworkConfig other)
    {
        Domain = other.Domain;
        UseSSL = other.UseSSL;
        Port = other.Port;
        STUNServer = other.STUNServer;
        TURNServer = other.TURNServer;
        ConnectionTimeout = other.ConnectionTimeout;
        MaxRetries = other.MaxRetries;
    }
    
    public string DomainUrl()
    {
        string url = "";
        
        url += this.UseSSL ? "https://" : "http://";
        url += this.Domain + ":" + this.Port;
        
        return url; 
    }
}