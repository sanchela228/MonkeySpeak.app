namespace App.System.Models.Websocket.Messages.AuthCall;

public class AddFriend : IMessage
{
    public string FriendUsername { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
