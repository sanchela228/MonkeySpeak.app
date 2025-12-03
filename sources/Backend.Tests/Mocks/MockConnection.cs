using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Core.Websockets;

namespace Backend.Tests.Mocks;

public class MockConnection() : Connection(new MockWebSocket())
{
    public List<IMessage> SentMessages { get; } = new();

    public override Task SendAsync(IMessage message)
    {
        SentMessages.Add(message);
        return Task.CompletedTask;
    }
    
    public T? GetLastMessage<T>() where T : IMessage
    {
        return SentMessages.OfType<T>().LastOrDefault();
    }
    
    public List<T> GetMessages<T>() where T : IMessage
    {
        return SentMessages.OfType<T>().ToList();
    }
    
    public void ClearMessages()
    {
        SentMessages.Clear();
    }
}

public class MockWebSocket : WebSocket
{
    public override WebSocketState State => WebSocketState.Open;
    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override string? SubProtocol => null;

    public override void Abort() { }
    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;
    public override void Dispose() { }
    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Text, true));
    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) => Task.CompletedTask;
}