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

    public CallFacade(INetworkConfig netConfig, WebSocketClient wsClient)
    {
        _netConfig = netConfig;
        _wsClient = wsClient;

        ISignalingClient signaling = new WebsocketSignalingClient(_wsClient);
        IStunClient stun = new MainServerStunClient();

        _engine = new P2PCallManager(signaling, stun, _netConfig);
        _engine.OnSessionStateChanged += CallStateHandler;
        _engine.OnConnected += HandleEngineConnected;
        
        _wsClient.MessageDispatcher.On<SessionCreated>(msg => OnSessionCreated?.Invoke(msg.Value));
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

    private void CallStateHandler(CallSession session, CallState state)
    {
        OnSessionStateChanged?.Invoke(session, state);
        if (state == CallState.Closed)
        {
            OnCallEnded?.Invoke();
        }
    }
    
    private void CallMuteHandler(bool isMuted) => OnRemoteMuteChanged?.Invoke(isMuted);

    public async void Hangup()
    {
        await HangupAsync(_engine.CurrentSession());
        Clear();
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
    public event Action OnCallEnded;
    public event Action<bool>? OnRemoteMuteChanged;

    public Task<CallSession> CreateSessionAsync() => _engine.CreateSessionAsync();
    public Task<CallSession> CreateSessionAsync(CancellationToken cancellationToken) => _engine.CreateSessionAsync(cancellationToken);
    public Task<CallSession> ConnectToSessionAsync(string code) => _engine.ConnectToSessionAsync(code);
    public Task<CallSession> ConnectToSessionAsync(string code, CancellationToken cancellationToken) => _engine.ConnectToSessionAsync(code, cancellationToken);
    public Task HangupAsync(CallSession session) => _engine.HangupAsync(session);

    public void Clear()
    {
        _engine.OnRemoteMuteChanged -= CallMuteHandler;
        _engine.OnSessionStateChanged -= CallStateHandler; 
    }

    private void HandleEngineConnected()
    {
        OnConnected?.Invoke();
        _engine.OnRemoteMuteChanged += CallMuteHandler;
    }
}
