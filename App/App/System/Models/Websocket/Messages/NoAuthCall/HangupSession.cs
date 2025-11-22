namespace App.System.Models.Websocket.Messages.NoAuthCall;

public class HangupSession : IMessage
{
    public string Value { get; set; }
}
