using System.Xml.Serialization;
using App.System;
using App.System.Services.CallServices;

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
    
    [XmlIgnore]
    public ICallService CallService { get; set; } = new P2P();
}