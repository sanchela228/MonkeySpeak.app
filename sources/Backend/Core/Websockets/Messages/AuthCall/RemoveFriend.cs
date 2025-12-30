namespace Core.Websockets.Messages.AuthCall;

public class RemoveFriend : IMessage
{
    public string FriendId { get; set; }
    public string Value { get; set; }
}
