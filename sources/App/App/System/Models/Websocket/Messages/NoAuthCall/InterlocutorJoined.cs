namespace App.System.Models.Websocket.Messages.NoAuthCall;

public class InterlocutorJoined : IMessage
{
    public string Id { get; set; }
    public string Value { get; set; }
    public string IpEndPoint { get; set; }
}