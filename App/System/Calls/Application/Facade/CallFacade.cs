using System;
using System.Threading.Tasks;
using System.Threading;
using App.Configurations.Interfaces;
using App.System.Calls.Application.Adapters;
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
    private Action<CallSession, CallState>? _engineStateHandler;

    public CallFacade(INetworkConfig netConfig, WebSocketClient wsClient)
    {
        _netConfig = netConfig;
        _wsClient = wsClient;

        ISignalingClient signaling = new WebsocketSignalingClient(_wsClient);
        IStunClient stun = new MainServerStunClient();
        IHolePuncher puncher = new UdpHolePuncher();

        _engine = new P2PCallManager(signaling, stun, puncher, _netConfig);

        _engineStateHandler = (session, state) => OnSessionStateChanged?.Invoke(session, state);
        _engine.OnSessionStateChanged += _engineStateHandler;

        _wsClient.MessageDispatcher.On<SessionCreated>(msg => OnSessionCreated?.Invoke(msg.Value));
        
        _engine.OnConnected += () => OnConnected?.Invoke();
    }

    private bool _microphoneEnabled = true;
    public bool MicrophoneEnabled
    {
        get { return _microphoneEnabled; }
        set
        {
            _microphoneEnabled = value;
            SetMicrophoneStatus(_microphoneEnabled);
        }
    }

    private void SetMicrophoneStatus(bool status)
    {
        _engine.SetMicrophoneStatus(status);
        
        // TODO: ADD SIGNALING EVENT FOR MUTE AND UNMUTE
    }

    public void StartAudioProcess() => _engine.StartAudioProcess();
    public event Action<CallSession, CallState>? OnSessionStateChanged;
    public event Action<string>? OnSessionCreated;
    public event Action OnConnected;

    public Task<CallSession> CreateSessionAsync() => _engine.CreateSessionAsync();
    public Task<CallSession> CreateSessionAsync(CancellationToken cancellationToken) => _engine.CreateSessionAsync(cancellationToken);
    public Task<CallSession> ConnectToSessionAsync(string code) => _engine.ConnectToSessionAsync(code);
    public Task<CallSession> ConnectToSessionAsync(string code, CancellationToken cancellationToken) => _engine.ConnectToSessionAsync(code, cancellationToken);
    public Task HangupAsync(CallSession session) => _engine.HangupAsync(session);

    public void Clear()
    {
        if (_engineStateHandler != null)
        {
            try { _engine.OnSessionStateChanged -= _engineStateHandler; } catch { }
            _engineStateHandler = null;
        }
    }
}
