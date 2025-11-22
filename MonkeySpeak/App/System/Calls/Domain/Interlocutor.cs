using System.Net;

namespace App.System.Calls.Domain;

public class Interlocutor
{
    public IPEndPoint RemoteIp;
    public CallState State;
    
    public Interlocutor(IPEndPoint remoteIp, CallState state)
    {
        this.RemoteIp = remoteIp;
        this.State = state;
    }
   
}