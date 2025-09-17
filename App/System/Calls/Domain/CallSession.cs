using System.Net;

namespace App.System.Calls.Domain;

public class CallSession
{
    public Guid CallId { get; }
    public string? PeerId { get; private set; }

    public CallState State { get; private set; } = CallState.Idle;
    public int LocalUdpPort { get; private set; }

    public IPEndPoint? PublicEndPoint { get; private set; }
    public IPEndPoint? LocalEndPoint { get; private set; }

    // TODO: CREATE PEER CLASS
    public List<string> Peers = [];
    
    
    // TODO: REMOVE THIS
    public IPEndPoint? PeerPublicEndPoint { get; private set; }
    public IPEndPoint? PeerLocalEndPoint { get; private set; }

    public CallSession(Guid? callId = null)
    {
        CallId = callId ?? Guid.NewGuid();
    }

    public void SetLocal(int localUdpPort, IPEndPoint? publicEp, IPEndPoint? localEp)
    {
        LocalUdpPort = localUdpPort;
        PublicEndPoint = publicEp;
        LocalEndPoint = localEp;
    }

    public void SetPeerEndpoints(IPEndPoint? peerPublic, IPEndPoint? peerLocal)
    {
        PeerPublicEndPoint = peerPublic;
        PeerLocalEndPoint = peerLocal;
    }

    public void TransitionTo(CallState newState)
    {
        State = newState;
    }
}
