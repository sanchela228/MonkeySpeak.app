using System.Net;
using App.Configurations.Interfaces;
using App.System.Calls.Application;
using App.System.Calls.Domain;
using App.System.Calls.Infrastructure;
using App.System.Models.Websocket;
using App.System.Models.Websocket.Messages.NoAuthCall;

namespace App.System.Calls.Application.Adapters.CallManagers;

public class P2PCallManager : ICallManager
{
    private readonly ISignalingClient _signaling;
    private readonly IStunClient _stun;
    private readonly IHolePuncher _puncher;
    private readonly CallConfig _config;
    private bool _signalingSubscribed;
    private CallSession? _activeSession;
    private bool _connectedRaised;
    private IPEndPoint _IPEndPoint;
    private int _localPort;

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, IHolePuncher puncher, CallConfig config)
    {
        _signaling = signaling;
        _stun = stun;
        _puncher = puncher;
        _config = config;
        
        _localPort = SelectLocalUdpPort();
        GetPublicEndPointAsync();
    }

    private async Task GetPublicEndPointAsync()
    {
        var ip = await _stun.GetPublicEndPointAsync(_localPort, _config.StunTimeoutMs);
        _IPEndPoint = ip;
    }

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, IHolePuncher puncher, INetworkConfig netConfig)
        : this(signaling, stun, puncher, new CallConfig(netConfig))
    {
    }

    public event Action<CallSession, CallState>? OnSessionStateChanged;

    public async Task<CallSession> CreateSessionAsync()
    {
        var session = new CallSession();
        Transition(session, CallState.Negotiating);
        
        session.SetLocal(_localPort, _IPEndPoint, null);
        Console.WriteLine($"PUBLIC IP CREATE: {_IPEndPoint}");

        EnsureSignalingSubscription();
        await _signaling.SendAsync(new CreateSession
        {
            Value = string.Empty,
            IpEndPoint = _IPEndPoint?.ToString() ?? string.Empty
        });

        _activeSession = session;
        return session;
    }

    public async Task<CallSession> ConnectToSessionAsync(string code)
    {
        var session = new CallSession();
        Transition(session, CallState.Negotiating);
        
        session.SetLocal(_localPort, _IPEndPoint, null);
        Console.WriteLine($"PUBLIC IP CONNECT: {_IPEndPoint}");

        EnsureSignalingSubscription();
        await _signaling.SendAsync(new ConnectToSession
        {
            Code = code,
            Value = code,
            IpEndPoint = _IPEndPoint?.ToString() ?? string.Empty
        });

        _activeSession = session;
        return session;
    }

    public async Task HangupAsync(CallSession session)
    {
        Transition(session, CallState.Closed);
        await Task.CompletedTask;
    }

    private static int SelectLocalUdpPort()
    {
        var rnd = new Random();
#if DEBUG
        return 5000 + rnd.Next(1000);
#else
        return 40000 + rnd.Next(20000);
#endif
    }

    private void Transition(CallSession session, CallState state)
    {
        session.TransitionTo(state);
        OnSessionStateChanged?.Invoke(session, state);
    }

    private void EnsureSignalingSubscription()
    {
        if (_signalingSubscribed) return;
        _signalingSubscribed = true;
        _signaling.OnMessage += HandleSignalingMessage;
        _puncher.OnData += HandlePuncherData;
    }

    private async void HandleSignalingMessage(Models.Websocket.Context ctx)
    {
        try
        {
            var msg = ctx.ToMessage();
            switch (msg)
            {
                case HolePunching hp:
                    if (_activeSession == null) return;
                    if (string.IsNullOrWhiteSpace(hp.IpEndPoint)) return;
                    if (!TryParseIpEndPoint(hp.IpEndPoint, out var remote)) return;

                    _activeSession.SetPeerEndpoints(remote, null);
                    Transition(_activeSession, CallState.HolePunching);

                    var cts = new CancellationTokenSource();
                    await _puncher.StartAsync(remote, _activeSession.LocalUdpPort, cts.Token);
                    break;
            }
        }
        catch
        {
        }
    }

    private void HandlePuncherData(byte[] data)
    {
        if (_activeSession == null) return;
        if (_connectedRaised) return;
        if (_activeSession.State == CallState.HolePunching)
        {
            _connectedRaised = true;
            Transition(_activeSession, CallState.Connected);
        }
    }

    private static bool TryParseIpEndPoint(string s, out IPEndPoint ep)
    {
        ep = null;
        try
        {
            var parts = s.Split(':');
            if (parts.Length != 2) return false;
            if (!IPAddress.TryParse(parts[0], out var ip)) return false;
            if (!int.TryParse(parts[1], out var port)) return false;
            ep = new IPEndPoint(ip, port);
            return true;
        }
        catch { return false; }
    }
}
