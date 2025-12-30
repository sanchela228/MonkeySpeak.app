namespace Core.Websockets.Messages.AuthCall;

public class InitiateCall : IMessage
{
    public string FriendId { get; set; }
    public string IpEndPoint { get; set; }
    public string Value { get; set; }
}
