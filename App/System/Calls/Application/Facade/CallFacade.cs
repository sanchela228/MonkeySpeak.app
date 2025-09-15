using System;
using System.Threading.Tasks;
using System.Threading;
using App.Configurations.Interfaces;
using App.System.Calls.Application.Adapters.CallManagers;
using App.System.Calls.Domain;
using App.System.Calls.Infrastructure;
using App.System.Calls.Infrastructure.Adapters;
using App.System.Modules;
using App.System.Models.Websocket.Messages.NoAuthCall;

namespace App.System.Calls.Application.Facade;

public class CallFacade
{
    private readonly INetworkConfig _netConfig;
    private readonly WebSocketClient _wsClient;

    private readonly ICallManager _engine;

    public CallFacade(INetworkConfig netConfig, WebSocketClient wsClient)
    {
        _netConfig = netConfig;
        _wsClient = wsClient;

        ISignalingClient signaling = new WebsocketSignalingClient(_wsClient);
        IStunClient stun = new GoogleStunClient();
        IHolePuncher puncher = new UdpHolePuncher();

        _engine = new P2PCallManager(signaling, stun, puncher, _netConfig);

        _engine.OnSessionStateChanged += (session, state) => OnSessionStateChanged?.Invoke(session, state);

        _wsClient.MessageDispatcher.On<SessionCreated>(msg => OnSessionCreated?.Invoke(msg.Value));
    }

    public event Action<CallSession, CallState>? OnSessionStateChanged;
    public event Action<string>? OnSessionCreated;

    public Task<CallSession> CreateSessionAsync() => _engine.CreateSessionAsync();
    public Task<CallSession> CreateSessionAsync(CancellationToken cancellationToken) => _engine.CreateSessionAsync(cancellationToken);
    public Task<CallSession> ConnectToSessionAsync(string code) => _engine.ConnectToSessionAsync(code);
    public Task<CallSession> ConnectToSessionAsync(string code, CancellationToken cancellationToken) => _engine.ConnectToSessionAsync(code, cancellationToken);
    public Task HangupAsync(CallSession session) => _engine.HangupAsync(session);
}
