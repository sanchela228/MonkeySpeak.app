using System.Net;
using System.Threading;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using App.Configurations.Roots;
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
    private bool _microphoneEnabled = true;
    public event Action<bool>? OnRemoteMuteChanged;
    public event Action<string, bool>? OnRemoteMuteChangedByInterlocutor;
    private readonly UdpControlService _controls = new UdpControlService();

    public CallSession CurrentSession() => _activeSession;

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, CallConfig config)
    {
        _signaling = signaling;
        _stun = stun;
        _config = config;
        
        _localPort = SelectLocalUdpPort();
    }

    public void SetVolumeStatus(bool status) => audioTranslator.TogglePlaybackAudio(status);

    public void SetMicrophoneVolumePercent(int percent)
    {
        percent = Math.Clamp(percent, 0, 200);

        if (audioTranslator != null)
        {
            audioTranslator.SetMicrophoneVolumePercent(percent);
        }
        else
        {
            Logger.Write(Logger.Type.Warning, "[P2P] SetMicrophoneVolumePercent: audioTranslator is null");
        }
    }

    public void SetPlaybackVolumePercent(int percent)
    {
        percent = Math.Clamp(percent, 0, 200);

        if (audioTranslator != null)
        {
            audioTranslator.SetPlaybackVolumePercent(percent);
        }
        else
        {
            Logger.Write(Logger.Type.Warning, "[P2P] SetPlaybackVolumePercent: audioTranslator is null");
        }
    }
    
    public void SetMicrophoneStatus(bool status)
    {
        _microphoneEnabled = status;
        Logger.Write(Logger.Type.Info, $"[P2P] SetMicrophoneStatus: {status}");
        
        if (audioTranslator != null)
        {
            audioTranslator.ToggleCaptureAudio(_microphoneEnabled);
        }
        else
        {
            Logger.Write(Logger.Type.Warning, "[P2P] audioTranslator is null in SetMicrophoneStatus");
        }
        
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

    public P2PCallManager(ISignalingClient signaling, IStunClient stun, NetworkConfig netConfig)
        : this(signaling, stun, new CallConfig(netConfig))
    {
    }
    
    public Dictionary<string, float> GetAudioLevels()
    {
        if (audioTranslator != null) 
            return audioTranslator.GetAudioLevels();
        
        return new Dictionary<string, float>();
    }

    public float GetSelfAudioLevel()
    {
        if (audioTranslator != null)
            return audioTranslator.GetSelfAudioLevel();

        return 0f;
    }

    public DeviceInfo[] GetCaptureDevices()
    {
        if (audioTranslator != null)
            return audioTranslator.GetCaptureDevices();

        return Array.Empty<DeviceInfo>();
    }
    
    public DeviceInfo[] GetPlaybackDevices()
    {
        if (audioTranslator != null)
            return audioTranslator.GetPlaybackDevices();

        return Array.Empty<DeviceInfo>();
    }

    public void SwitchCaptureDevice(IntPtr? deviceId)
    {
        if (audioTranslator == null)
        {
            Logger.Write(Logger.Type.Warning, "[P2P] SwitchCaptureDevice: audioTranslator is null");
            return;
        }

        audioTranslator.SwitchCaptureDevice(deviceId);
    }
    
    public void SwitchPlaybackDevice(IntPtr? deviceId)
    {
        if (audioTranslator == null)
        {
            Logger.Write(Logger.Type.Warning, "[P2P] SwitchPlaybackDevice: audioTranslator is null");
            return;
        }

        audioTranslator.SwitchPlaybackDevice(deviceId);
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
        Transition(session, CallState.Idle);
        
        var localLanEp = GetLocalLanEndpoint(_localPort);
        IPEndPoint publicEp = null;
        
#if DEBUG
        publicEp = localLanEp;
#else
        publicEp = await _stun.GetPublicEndPointAsync(_localPort, _config.StunTimeoutMs, cancellationToken, Context.Network.Config.Domain);
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
        Transition(session, CallState.Idle);
        
        var localLanEp = GetLocalLanEndpoint(_localPort);
        IPEndPoint publicEp = null;
        
#if DEBUG
        publicEp = localLanEp;
#else
        publicEp = await _stun.GetPublicEndPointAsync(_localPort, _config.StunTimeoutMs, cancellationToken, Context.Network.Config.Domain);
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
        _signaling?.SendAsync(new HangupSession());

        try
        {
            audioTranslator?.Dispose();

            await _udpCts?.CancelAsync();
            _udpManager?.Stop();

            if (_udpManager != null)
            {
                _udpManager.OnHolePunchData -= HandlePuncherData;
                _udpManager.OnConnected -= HandleOnConnected;
            }

            try
            {
                _controls.OnRemoteMuteChangedByInterlocutor -= HandleRemoteMuteChangedByInterlocutor;
            }
            catch { }

            _controls.Detach();
            _udpCts?.Dispose();

            if (_signalingSubscribed)
            {
                _signaling.OnMessage -= HandleSignalingMessage;
                _signalingSubscribed = false;
            }

        }
        catch (Exception ex)
        {
            Logger.Error($"[P2P] Hangup: {ex}");
        }
        finally
        {
            _udpManager = null;
            audioTranslator = null;
            _udpCts = null;
            _connectedRaised = false;
            _activeSession = null;
            _audioProcessStartRequested = false;
        }
       
        await Task.CompletedTask;
    }

    private AudioTranslator audioTranslator;
    private bool _audioProcessStartRequested = false;
    
    public void StartAudioProcess()
    {
        _audioProcessStartRequested = true;
        TryInitializeAudioTranslator();
    }
    
    private void TryInitializeAudioTranslator()
    {
        try
        {
            if (!_audioProcessStartRequested)
            {
                return;
            }
            
            if (audioTranslator != null)
            {
                Logger.Write(Logger.Type.Info, "[AudioTranslator] Already initialized");
                return;
            }
            
            if (_udpManager == null)
            {
                Logger.Write(Logger.Type.Warning, "[AudioTranslator] UDP manager not ready yet, waiting...");
                return;
            }
            
            audioTranslator = new AudioTranslator(_udpManager, new CancellationTokenSource());
            
            Logger.Write(Logger.Type.Info, "[AudioTranslator] Successfully initialized");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Initialization error: {ex.Message}", ex);
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
        
        _controls.Send(ControlCode.MuteState, (byte)(_microphoneEnabled ? 1 : 0));
        OnConnected?.Invoke();
    }
    
    private void HandleSignalingMessage(Models.Websocket.Context ctx)
    {
        try
        {
            if (_activeSession == null)
            {
                Logger.Write(Logger.Type.Warning, "[P2P] Received signaling message but session is null, ignoring");
                return;
            }
            
            var msg = ctx.ToMessage();
            switch (msg)
            {
                case InterlocutorJoined joined:
                    Logger.Write($"[P2P] InterlocutorJoined event: {joined.Id}");
                    
                    if (_activeSession == null) 
                        return;
                    
                    if (string.IsNullOrWhiteSpace(joined.IpEndPoint)) 
                        return;
                    
                    if (!TryParseIpEndPoint(joined.IpEndPoint, out var joinRemote)) 
                        return;

                    if (_udpManager == null)
                    {
                        _udpManager = new UdpUnifiedManager();
                        _udpManager.OnHolePunchData += HandlePuncherData;
                        _udpManager.OnConnected += HandleOnConnected;
                        _udpCts = new CancellationTokenSource();
                        _udpManager.StartWithClient(new UdpClient(_activeSession.LocalUdpPort), joinRemote, _udpCts.Token);

                        _controls.Attach(_udpManager);

                        _controls.OnRemoteMuteChangedByInterlocutor += HandleRemoteMuteChangedByInterlocutor;

                        TryInitializeAudioTranslator();
                    }

                    try 
                    { 
                        _udpManager.AddInterlocutor(joined.Id, joinRemote);
                        Logger.Write($"[P2P] Added interlocutor to UDP: {joined.Id.Substring(0, Math.Min(8, joined.Id.Length))}");
                    } 
                    catch (Exception ex)
                    {
                        Logger.Error($"[P2P] Failed to add interlocutor to UDP: {ex.Message}");
                    }

                    if (_activeSession.Interlocutors.All(x => x.Id != joined.Id))
                    {
                        _activeSession.Interlocutors.Add(new Interlocutor(joined.Id, joinRemote, CallState.Connected));
                        
                        var interlocutorId = joined.Id;
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(50);
                            
                            try
                            {
                                _controls?.SendMuteStateToInterlocutor(_microphoneEnabled, interlocutorId);
                                Logger.Write($"[P2P] Sent mute state ({_microphoneEnabled}) to new interlocutor {interlocutorId}");
                            }
                            catch (Exception ex)
                            {
                                Logger.Write(Logger.Type.Warning, $"[P2P] Failed to send mute state to {interlocutorId}: {ex.Message}");
                            }
                        });
                    }
                    break;

                case InterlocutorLeft left:
                    Logger.Write($"[P2P] InterlocutorLeft event: {left.InterlocutorId}");
                    
                    if (_activeSession == null) 
                        return;
                    
                    try { _udpManager?.RemoveInterlocutor(left.InterlocutorId); } 
                    catch { }
                    
                    try { audioTranslator?.RemoveInterlocutorChannel(left.InterlocutorId); } 
                    catch { }
                    
                    for (int i = _activeSession.Interlocutors.Count - 1; i >= 0; i--)
                    {
                        if (_activeSession.Interlocutors[i].Id == left.InterlocutorId)
                            _activeSession.Interlocutors.RemoveAt(i);
                    }
                    
                    if (_activeSession.Interlocutors.Count == 0)
                    {
                        Logger.Write($"[P2P] No other participants left, ending call");
                        _ = Task.Run(async () => await HangupAsync(_activeSession));
                    }
                    break;

                case ConnectedToSession connectedToSession:
                    if (_activeSession == null) return;
                    Transition(_activeSession, CallState.Connected);
                    break;

                case SessionCreated sessionCreated:
                    if (_activeSession == null) return;
                    Transition(_activeSession, CallState.Waiting);
                    break;
                
                case ErrorConnectToSession errorConnectToSession:
                    if (_activeSession == null) return;
                    Transition(_activeSession, CallState.Failed);
                    break;
                
                default: Logger.Error($"[HandleSignalingMessage] Unknown message: {msg}"); break;
            }
        }
        catch (Exception messageReadEx)
        {
            Logger.Error($"[P2P] Failed to handle signaling message: {messageReadEx.Message}");
            Logger.Error($"[P2P] Stack trace: {messageReadEx.StackTrace}");
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

    private void HandleRemoteMuteChanged(bool isMuted)
    {
        Logger.Write(Logger.Type.Warning, $"[P2P] HandleRemoteMuteChanged (OLD, no ID): isMuted={isMuted}. This should NOT be used in mesh!");
        OnRemoteMuteChanged?.Invoke(isMuted);
    }
    
    private void HandleRemoteMuteChangedByInterlocutor(string interlocutorId, bool isMuted)
    {
        Logger.Write(Logger.Type.Info, $"[P2P] HandleRemoteMuteChangedByInterlocutor: {interlocutorId.Substring(0, Math.Min(8, interlocutorId.Length))}, isMuted={isMuted}");
        OnRemoteMuteChangedByInterlocutor?.Invoke(interlocutorId, isMuted);
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
