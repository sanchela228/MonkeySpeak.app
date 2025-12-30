namespace Core.Websockets.Messages.AuthCall;

public class ErrorRegistration : IMessage
{
    public string Value { get; set; }
    public string ErrorCode { get; set; }
}
