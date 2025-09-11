using App.System;

namespace App.Configurations.Interfaces;

public interface INetworkConfig
{
    string Domain { get; set; }
    bool UseSSL { get; set; }
    int Port { get; set; }
    string STUNServer { get; set; }
    string TURNServer { get; set; }
    int ConnectionTimeout { get; set; }
    int MaxRetries { get; set; }

    string DomainUrl();
}