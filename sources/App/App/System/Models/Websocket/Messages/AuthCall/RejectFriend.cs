namespace App.System.Models.Websocket.Messages.AuthCall;

public class RejectFriend : IMessage
{
    public string FriendshipId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
