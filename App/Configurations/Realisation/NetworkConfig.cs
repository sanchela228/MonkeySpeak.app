using System.Xml.Serialization;
using App.Configurations.Interfaces;
using App.System;

namespace App.Configurations.Realisation;

public class NetworkConfig : INetworkConfig
{
    public string Domain { get; set; }
    public bool UseSSL { get; set; }
    public int Port { get; set; }
    public string STUNServer { get; set; }
    public string TURNServer { get; set; }
    public int ConnectionTimeout { get; set; }
    public int MaxRetries { get; set; }
    
    public string DomainUrl()
    {
        string url = "";
        
        url += this.UseSSL ? "https://" : "http://";
        url += this.Domain + ":" + this.Port;
        
        return url; 
    }
}