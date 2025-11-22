namespace App.System.Models.Websocket.Messages;

public class Ping : IMessage
{
    public string Value { get; set; } = "ping";
}