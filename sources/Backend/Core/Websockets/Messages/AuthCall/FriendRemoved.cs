namespace Core.Websockets.Messages.AuthCall;

public class FriendRemoved : IMessage
{
    public string FriendId { get; set; }
    public string Value { get; set; }
}
