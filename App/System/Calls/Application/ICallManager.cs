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
    
    public event Action<CallSession, CallState>? OnSessionStateChanged;
    public event Action OnConnected;
}
