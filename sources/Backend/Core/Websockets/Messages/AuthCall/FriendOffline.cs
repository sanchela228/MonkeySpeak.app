namespace Core.Websockets.Messages.AuthCall;

public class FriendOffline : IMessage
{
    public string FriendId { get; set; }
    public string FriendUsername { get; set; }
    public string Value { get; set; }
}
