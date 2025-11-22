namespace App.System.Models.Websocket.Messages.NoAuthCall;

public class ErrorConnectToSession : IMessage
{
    public string Value { get; set; }
}
