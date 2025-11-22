using System.Net;

namespace Core.Websockets.Messages.NoAuthCall;

public class HolePunching : IMessage
{
    public string Value { get; set; }
    public string IpEndPoint { get; set; }
}