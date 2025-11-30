namespace Core.Websockets.Messages.NoAuthCall;
public class InterlocutorLeft : IMessage
{
    public string Value { get; set; }
    public string InterlocutorId { get; set; }
}