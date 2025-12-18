using App.System.Calls.Domain;
using System.Threading;
using SoundFlow.Structs;

namespace App.System.Calls.Application;

public interface ICallManager
{
    Task<CallSession> CreateSessionAsync();
    Task<CallSession> CreateSessionAsync(CancellationToken cancellationToken);
    Task<CallSession> ConnectToSessionAsync(string code);
    Task<CallSession> ConnectToSessionAsync(string code, CancellationToken cancellationToken);
    Task HangupAsync(CallSession session);
    public CallSession CurrentSession();
    void SetMicrophoneStatus(bool status);
    void SetMicrophoneVolumePercent(int percent);
    void SetVolumeStatus(bool status);
    void SetPlaybackVolumePercent(int percent);

    void StartAudioProcess();
    Dictionary<string, float> GetAudioLevels();
    float GetSelfAudioLevel();

    DeviceInfo[] GetCaptureDevices();
    DeviceInfo[] GetPlaybackDevices();
    void SwitchCaptureDevice(IntPtr? deviceId);
    void SwitchPlaybackDevice(IntPtr? deviceId);
    public event Action<CallSession, CallState>? OnSessionStateChanged;
    public event Action OnConnected;
    public event Action<bool>? OnRemoteMuteChanged;
    public event Action<string, bool>? OnRemoteMuteChangedByInterlocutor;
}
