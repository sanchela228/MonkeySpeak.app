using System.Net;
using Core.Websockets;

namespace Core.Websockets.Messages.NoAuthCall;

public class ConnectedToSession : IMessage
{
    public List<string> ListConnections { get; set; }
    public string Value { get; set; }
}