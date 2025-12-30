namespace App.System.Models.Websocket.Messages.AuthCall;

public class GetFriendList : IMessage
{
    public string Value { get; set; } = string.Empty;
}
