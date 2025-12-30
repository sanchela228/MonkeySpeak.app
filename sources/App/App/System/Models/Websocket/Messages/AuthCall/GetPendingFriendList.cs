namespace App.System.Models.Websocket.Messages.AuthCall;

public class GetPendingFriendList : IMessage
{
    public string Value { get; set; } = string.Empty;
}