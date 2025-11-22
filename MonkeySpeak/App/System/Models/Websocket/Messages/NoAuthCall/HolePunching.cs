using System.Net;

namespace App.System.Models.Websocket.Messages.NoAuthCall;

public class HolePunching : IMessage
{
    public string Value { get; set; }
    public string IpEndPoint { get; set; }
}