namespace Core.Websockets.Messages.AuthCall;

public class GetPendingFriendList : IMessage
{
    public string Value { get; set; } = string.Empty;
}