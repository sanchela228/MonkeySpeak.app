using System.Net;
using Core.Websockets;

namespace Core.Websockets.Messages.NoAuthCall;

public class ConnectToSession : IMessage
{
    public string Code { get; set; }
    public string Value { get; set; }
    
    public string IpEndPoint { get; set; }
}