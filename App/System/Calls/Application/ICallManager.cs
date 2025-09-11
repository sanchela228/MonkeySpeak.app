using App.System.Calls.Domain;

namespace App.System.Calls.Application;

public interface ICallManager
{
    Task<CallSession> CreateSessionAsync();
    Task<CallSession> ConnectToSessionAsync(string code);
    Task HangupAsync(CallSession session);
    
    public event Action<CallSession, CallState>? OnSessionStateChanged;
}
