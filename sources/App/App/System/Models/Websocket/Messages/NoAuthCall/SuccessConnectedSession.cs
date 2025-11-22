namespace App.System.Models.Websocket.Messages.NoAuthCall;

public class SuccessConnectedSession : IMessage
{
    public string Value { get; set; } = "";
}