namespace App.System.Models.Websocket.Messages.NoAuthCall;

public class SessionCreated : IMessage
{
    public string Value { get; set; }
}