namespace App.System.Models.Websocket.Messages;

public class ConnectToNoAuthCallSession : IMessage
{
    public string Value { get; set; }
}