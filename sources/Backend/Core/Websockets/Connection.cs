using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Core.Websockets;

public class Connection
{
    public readonly Guid Id;
    public readonly WebSocket WebSocket;
    
    public string PublicIp { get; set; }

    public StatusConnection Status { get; set; }

    public Connection(WebSocket WebSocket)
    {
        Id = Guid.NewGuid();
        Status = StatusConnection.Idle;
        this.WebSocket = WebSocket;
    }

    public async void Send(IMessage message)
    {
        var context = Context.Create(message);
        var buffer = Encoding.UTF8.GetBytes( JsonSerializer.Serialize<Context>(context) );
        
        await WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    
    public enum StatusConnection
    {
        Idle,
        Connecting,
        Connected,
    }
}