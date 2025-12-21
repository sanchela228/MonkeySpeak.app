namespace Core.Websockets.Messages.AuthCall;

public class AcceptFriend : IMessage
{
    public string FriendshipId { get; set; }
    public string Value { get; set; }
}
