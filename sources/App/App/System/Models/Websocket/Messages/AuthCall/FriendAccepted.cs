namespace App.System.Models.Websocket.Messages.AuthCall;

public class FriendAccepted : IMessage
{
    public string FriendshipId { get; set; } = string.Empty;
    public string FriendUserId { get; set; } = string.Empty;
    public string FriendUsername { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
