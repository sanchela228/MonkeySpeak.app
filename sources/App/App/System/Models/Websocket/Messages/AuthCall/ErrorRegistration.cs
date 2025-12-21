namespace App.System.Models.Websocket.Messages.AuthCall;

public class ErrorRegistration : IMessage
{
    public string Value { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}
