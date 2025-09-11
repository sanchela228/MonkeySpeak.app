using App.Configurations.Interfaces;

namespace App.System.Calls.Application;

public class CallConfig
{
    public string StunServer { get; init; } = "stun.l.google.com";
    public int StunPort { get; init; } = 19302;
    public int StunTimeoutMs { get; init; } = 5000;

    public int PunchAttemptIntervalMs { get; init; } = 1000;
    public int PunchMaxAttempts { get; init; } = 30;
    public int PunchKeepAliveMs { get; init; } = 15000;

    public int TransportReceiveTimeoutMs { get; init; } = 2000;

    public bool DisableUdpConnReset { get; init; } = true;

    public CallConfig(INetworkConfig config)
    {
        
    }

    public CallConfig() { }
}
