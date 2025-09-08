namespace App.System;

public interface ICallService
{
    void Connect();
    void CreateSession();
    event Action<string> OnSessionCreated;
}