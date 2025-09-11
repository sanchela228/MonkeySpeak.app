using System.Net;

namespace App.System.Models.Websocket.Messages.NoAuthCall;

public class ConnectedToSession : IMessage
{
    public List<string> ListConnections { get; set; }
    public string Value { get; set; }
}