namespace Core.Websockets.Messages.AuthCall;

public class FriendRequestRejected : IMessage
{
    public string FriendshipId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
