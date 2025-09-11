namespace App.System;

public interface ICallService
{
    Task Connect(string session);
    Task CreateSession();
    event Action<string> OnSessionCreated;
    event Action<string> OnSessionConnected;
}