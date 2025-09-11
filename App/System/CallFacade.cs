using App.Configurations.Interfaces;

namespace App.System;

public class CallFacade(INetworkConfig config)
{
    public ICallService Service { get; private set; } = config.CallService;
    public CallState State { get; private set; } = CallState.Idle;
    
    public async Task CreateSession()
    {
        State = CallState.WaitingConnect;
        Service.CreateSession();
    }

    public async Task ConnectToSession(string session)
    {
        Console.WriteLine("ConnectToSession");
        State = CallState.Connecting;
        Service.Connect(session);
    }

    public enum CallState
    {
        Idle,
        WaitingConnect,
        Connected,
        Connecting,
        Error
    }
}