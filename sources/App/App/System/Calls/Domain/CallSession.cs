using System.Collections.ObjectModel;
using System.Net;

namespace App.System.Calls.Domain;

public class CallSession
{
    public Interlocutor Self { get; private set; }

    public CallState State { get; private set; } = CallState.Idle;
    public int LocalUdpPort { get; private set; }
    public IPEndPoint? PublicEndPoint { get; private set; }
    public IPEndPoint? LocalEndPoint { get; private set; }

    public ObservableCollection<Interlocutor> Interlocutors = [];
    
    public void SetLocal(int localUdpPort, IPEndPoint? publicEp, IPEndPoint? localEp)
    {
        LocalUdpPort = localUdpPort;
        PublicEndPoint = publicEp;
        LocalEndPoint = localEp;
    }

    public void TransitionTo(CallState newState)
    {
        State = newState;
    }
}
