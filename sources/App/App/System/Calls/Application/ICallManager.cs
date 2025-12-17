using App.System.Calls.Domain;
using System.Threading;

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
    void SetVolumeStatus(bool status);

    void StartAudioProcess();
    Dictionary<string, float> GetAudioLevels();
    
    public event Action<CallSession, CallState>? OnSessionStateChanged;
    public event Action OnConnected;
    public event Action<bool>? OnRemoteMuteChanged;
    public event Action<string, bool>? OnRemoteMuteChangedByInterlocutor;
}
