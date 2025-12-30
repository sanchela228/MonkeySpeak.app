namespace Core.Websockets.Messages.AuthCall;

public class GetPublicKeys : IMessage
{
    public string UserId { get; set; }
    public string Value { get; set; }
}
