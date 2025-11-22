using System.Threading.Tasks;
using App.System.Calls.Infrastructure;
using App.System.Models.Websocket;
using App.System.Models.Websocket.Messages;
using App.System.Modules;

namespace App.System.Calls.Infrastructure.Adapters;

public class WebsocketSignalingClient : ISignalingClient
{
    private readonly WebSocketClient _client;

    public WebsocketSignalingClient(WebSocketClient client)
    {
        _client = client;
        _client.OnMessageReceived += c => OnMessage?.Invoke(c);
    }

    public event Action<App.System.Models.Websocket.Context> OnMessage;

    public Task ConnectAsync()
    {
        return _client.ConnectAsync();
    }

    public Task SendAsync(IMessage message)
    {
        return _client.SendAsync(message);
    }
}
