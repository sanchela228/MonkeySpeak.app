namespace Core.Websockets.Messages.AuthCall;

public class AddFriend : IMessage
{
    public string FriendUsername { get; set; }
    public string Value { get; set; }
}
