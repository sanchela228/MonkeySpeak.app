using System.Net;
using System.Threading;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using App.Configurations.Interfaces;
using App.System.Calls.Application;
using App.System.Calls.Domain;
using App.System.Calls.Infrastructure;
using App.System.Calls.Media;
using App.System.Models.Websocket;
using App.System.Models.Websocket.Messages.NoAuthCall;
using App.System.Services;
using App.System.Utils;
using Concentus.Enums;
using Concentus.Structs;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace App.System.Calls.Application.Adapters;

public class P2PCallManager : ICallManager
{
    private readonly ISignalingClient _signaling;
    private readonly IStunClient _stun;
    private readonly IHolePuncher _puncher;
    private readonly CallConfig _config;
    private bool _signalingSubscribed;
    private CallSession? _activeSession;
    private bool _connectedRaised;
    private int _localPort;
    private UdpClient _udpHolePunchClient;
    private UdpClient _udpAudioClient;

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, IHolePuncher puncher, CallConfig config)
    {
        _signaling = signaling;
        _stun = stun;
        _puncher = puncher;
        _config = config;
        
        _localPort = SelectLocalUdpPort();
        _udpAudioClient = new UdpClient(_localPort + 1);
    }

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, IHolePuncher puncher, INetworkConfig netConfig)
        : this(signaling, stun, puncher, new CallConfig(netConfig))
    {
    }

    public event Action<CallSession, CallState>? OnSessionStateChanged;
    public event Action OnConnected;

    public async Task<CallSession> CreateSessionAsync()
    {
        return await CreateSessionAsync(CancellationToken.None);
    }

    public async Task<CallSession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var session = new CallSession();
        Transition(session, CallState.Negotiating);
        
        var localLanEp = GetLocalLanEndpoint(_localPort);
        IPEndPoint publicEp = null;
        
#if DEBUG
        publicEp = localLanEp;
#else
        publicEp = await _stun.GetPublicEndPointAsync(_localPort, _config.StunTimeoutMs, cancellationToken, Context.Instance.Network.Config.Domain);
#endif
        
        if (publicEp is not null)
        {
            Logger.Write($"[P2P] PUBLIC IP CONNECT: {publicEp}; LOCAL LAN: {localLanEp}");
            
            session.SetLocal(_localPort, publicEp, localLanEp);
            
            EnsureSignalingSubscription();
            await _signaling.SendAsync(new CreateSession
            {
                Value = string.Empty,
                IpEndPoint = (publicEp ?? localLanEp)?.ToString() ?? string.Empty
            });
            
            _activeSession = session;
            return session;
        }
        
        Transition(session, CallState.Idle);
        return null;
    }

    public async Task<CallSession> ConnectToSessionAsync(string code)
    {
        return await ConnectToSessionAsync(code, CancellationToken.None);
    }

    public async Task<CallSession> ConnectToSessionAsync(string code, CancellationToken cancellationToken)
    {
        var session = new CallSession();
        Transition(session, CallState.Negotiating);
        
        var localLanEp = GetLocalLanEndpoint(_localPort);
        IPEndPoint publicEp = null;
        
#if DEBUG
        publicEp = localLanEp;
#else
        publicEp = await _stun.GetPublicEndPointAsync(_localPort, _config.StunTimeoutMs, cancellationToken, Context.Instance.Network.Config.Domain);
#endif

        if (publicEp is not null)
        {
            Logger.Write($"[P2P] PUBLIC IP CONNECT: {publicEp}; LOCAL LAN: {localLanEp}");
            
            session.SetLocal(_localPort, publicEp, localLanEp);
            
            EnsureSignalingSubscription();
            await _signaling.SendAsync(new ConnectToSession
            {
                Code = code,
                Value = code,
                IpEndPoint = (publicEp ?? localLanEp)?.ToString() ?? string.Empty
            });
            
            _activeSession = session;
            return session;
        }

        Transition(session, CallState.Idle);
        return null;
    }

    public async Task HangupAsync(CallSession session)
    {
        Transition(session, CallState.Closed);
        try { _udpHolePunchClient?.Close(); } catch { }
        _udpHolePunchClient = null;
        await Task.CompletedTask;
    }

    private AudioTranslator audioTranslator;

    public async Task StartAudioProcess()
    {
        var newRemote = new IPEndPoint(_activeSession.Interlocutors[0].RemoteIp.Address, _activeSession.Interlocutors[0].RemoteIp.Port + 1);
        // Logger.Write(Logger.Type.Info, $"StartAudioProcess UDP REMOTE newRemote: {test}");
        Console.WriteLine($"StartAudioProcess UDP REMOTE newRemote: {_activeSession.Interlocutors[0].RemoteIp.Port + 1}");
        
        audioTranslator = new AudioTranslator(_udpAudioClient, newRemote, new CancellationTokenSource());
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
        Logger.Write($"[P2P] TransitionTo: {state}");
        
        session.TransitionTo(state);
        OnSessionStateChanged?.Invoke(session, state);
    }

    private void EnsureSignalingSubscription()
    {
        if (_signalingSubscribed) return;
        _signalingSubscribed = true;
        
        _signaling.OnMessage += HandleSignalingMessage;
        _puncher.OnData += HandlePuncherData;
        _puncher.OnConnected += HandleOnConnected;
    }

    private void HandleOnConnected(IPEndPoint localIP, IPEndPoint remoteIP)
    {
        Transition(_activeSession, CallState.Connected);
        _signaling.SendAsync(new SuccessConnectedSession());
        
        // Task.Run(() => { StartAudioProcess(); });
        
        OnConnected?.Invoke();
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

                    
                    _activeSession.SetInterlocutor(new Interlocutor(remote, CallState.HolePunching));
                    Transition(_activeSession, CallState.HolePunching);
                    
                    if (_udpHolePunchClient == null)
                    {
                        _udpHolePunchClient = new UdpClient(_activeSession.LocalUdpPort);
                    }
                    var cts = new CancellationTokenSource();
                    await _puncher.StartWithClientAsync(_udpHolePunchClient, remote, cts.Token);
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

    private static IPEndPoint? GetLocalLanEndpoint(int port)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var addr = ipProps.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (addr != null)
                {
                    return new IPEndPoint(addr.Address, port);
                }
            }
        }
        catch
        {
        }
        return null;
    }
}
