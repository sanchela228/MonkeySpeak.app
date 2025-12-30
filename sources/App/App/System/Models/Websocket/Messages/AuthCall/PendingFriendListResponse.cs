namespace App.System.Models.Websocket.Messages.AuthCall;

public class PendingFriendListResponse : IMessage
{
    public List<FriendRequestReceived> Friends { get; set; } = new();
    public string Value { get; set; } = string.Empty;
}