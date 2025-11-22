using Core.Websockets;

namespace Core.Websockets.Messages;

public class Ping : IMessage
{
    public string Value { get; set; } = "ping";
}