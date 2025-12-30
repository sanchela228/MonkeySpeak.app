namespace Core.Websockets.Messages.AuthCall;

public class IncomingCall : IMessage
{
    public string RoomCode { get; set; }
    public string FromUserId { get; set; }
    public string FromUsername { get; set; }
    public string Value { get; set; }
}
