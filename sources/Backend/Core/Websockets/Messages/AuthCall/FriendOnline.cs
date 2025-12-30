namespace Core.Websockets.Messages.AuthCall;

public class FriendOnline : IMessage
{
    public string FriendId { get; set; }
    public string FriendUsername { get; set; }
    public string Value { get; set; }
}
