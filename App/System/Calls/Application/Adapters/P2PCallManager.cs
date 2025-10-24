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
using App.System.Calls.Application.Controls;
using App.System.Models.Websocket;
using App.System.Models.Websocket.Messages.NoAuthCall;
using App.System.Services;
using App.System.Managers;
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
    private readonly CallConfig _config;
    private bool _signalingSubscribed;
    private CallSession? _activeSession;
    private bool _connectedRaised;
    private int _localPort;
    private UdpUnifiedManager _udpManager;
    private CancellationTokenSource _udpCts;
    private bool _microphoneEnabled;
    public event Action<bool>? OnRemoteMuteChanged;
    private readonly UdpControlService _controls = new UdpControlService();

    public CallSession CurrentSession() => _activeSession;

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, CallConfig config)
    {
        _signaling = signaling;
        _stun = stun;
        _config = config;
        
        _localPort = SelectLocalUdpPort();
    }
    
    public void SetMicrophoneStatus(bool status)
    {
        _microphoneEnabled = status;
        audioTranslator.ToggleCaptureAudio(_microphoneEnabled);
        _controls.Send(ControlCode.MuteState, (byte)(_microphoneEnabled ? 1 : 0));
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

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, INetworkConfig netConfig)
        : this(signaling, stun, new CallConfig(netConfig))
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
        // Move to Closed state first
        Transition(session, CallState.Closed);

        // Stop audio processing thread/devices
        try { audioTranslator?.Dispose(); } catch { }
        audioTranslator = null;

        // Stop UDP and cancel background tasks
        try { _udpCts?.Cancel(); } catch { }
        try { _udpManager?.Stop(); } catch { }

        // Detach UDP events to avoid callbacks after hangup
        try
        {
            if (_udpManager != null)
            {
                _udpManager.OnHolePunchData -= HandlePuncherData;
                _udpManager.OnConnected -= HandleOnConnected;
            }
        }
        catch { }

        // Detach control service
        try { _controls.Detach(); } catch { }

        _udpManager = null;
        try { _udpCts?.Dispose(); } catch { }
        _udpCts = null;

        // Unsubscribe signaling to avoid stale messages for a dead session
        try
        {
            if (_signalingSubscribed)
            {
                _signaling.OnMessage -= HandleSignalingMessage;
                _signalingSubscribed = false;
            }
        }
        catch { }

        // Reset state for a clean next session
        _connectedRaised = false;
        _activeSession = null;

        await Task.CompletedTask;
    }

    private AudioTranslator audioTranslator;
    public void StartAudioProcess()
    {
        try
        {
            if (_udpManager == null)
            {
                Logger.Write(Logger.Type.Error, "[StartAudioProcess] UDP manager is not initialized");
                return;
            }
            audioTranslator = new AudioTranslator(_udpManager, new CancellationTokenSource());
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[StartAudioProcess] Error: {ex.Message}", ex);
        }
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
    }

    private void HandleOnConnected(IPEndPoint localIP, IPEndPoint remoteIP)
    {
        Transition(_activeSession, CallState.Connected);
        _signaling.SendAsync(new SuccessConnectedSession());
        
        // Task.Run(() => { StartAudioProcess(); });
        
        // Send our current mic state so remote UI syncs immediately
        _controls.Send(ControlCode.MuteState, (byte)(_microphoneEnabled ? 1 : 0));
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
                    
                    if (_udpManager == null)
                    {
                        _udpManager = new UdpUnifiedManager();
                        _udpManager.OnHolePunchData += HandlePuncherData;
                        _udpManager.OnConnected += HandleOnConnected;
                        _udpCts = new CancellationTokenSource();
                        _udpManager.StartWithClient(new UdpClient(_activeSession.LocalUdpPort), remote, _udpCts.Token);

                        // Attach control service
                        _controls.Attach(_udpManager);
                        _controls.OnRemoteMuteChanged += (isMuted) => OnRemoteMuteChanged?.Invoke(isMuted);
                    }
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
