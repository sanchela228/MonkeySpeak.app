using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Configurations.Roots;
using App.System.Managers;
using App.System.Models.Websocket;
using App.System.Models.Websocket.Messages;
using App.System.Services;

namespace App.System.Modules;

public class WebSocketClient(NetworkConfig conf)
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cts;
    private Uri _uri;

    public MessageDispatcher MessageDispatcher { get; private set; } = new MessageDispatcher();

    public event Action<App.System.Models.Websocket.Context> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnReconnecting;
    public event Action OnDisconnected;

    public async Task ConnectAsync()
    {
        _uri = new Uri($"ws://{conf.Domain}:{conf.Port}/connector");
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        try
        {
            await _webSocket.ConnectAsync(_uri, _cts.Token);

            if (_webSocket.State == WebSocketState.Open)
            {
                OnConnected?.Invoke();
                
                _ = Task.Run(ReceiveMessagesAsync, _cts.Token);
                // _ = Task.Run(SendPingAsync, _cts.Token);
            }
        }
        catch (Exception ex)
        {
            OnDisconnected?.Invoke();
            Task.Delay(1000);
            
            await ReconnectAsync();
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[4096];
        
        while (_webSocket?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Logger.Write("Server requested close");
                    await CloseAsync(WebSocketCloseStatus.NormalClosure, "Server requested close");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    App.System.Models.Websocket.Context context = JsonSerializer.Deserialize<App.System.Models.Websocket.Context>(json);

                    if (context is not null)
                        OnMessageReceived?.Invoke(context);
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                Logger.Write("Connection refused");
                await ReconnectAsync();
                break;
            }
            catch (OperationCanceledException)
            {
                Logger.Write("OperationCanceledException");
                break;
            }
            catch (Exception ex)
            {
                Logger.Write($"Error get: {ex.Message}");
                await ReconnectAsync();
                break;
            }
        }
    }

    private async Task SendPingAsync()
    {
        while (_webSocket?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(25), _cts.Token);
                
                if (_webSocket.State == WebSocketState.Open)
                    SendAsync(new Ping());
            }
            catch
            {
                
            }
        }
    }

    public async Task SendAsync(IMessage message)
    {
        var context = App.System.Models.Websocket.Context.Create(message);
        
        if (_webSocket?.State != WebSocketState.Open)
        {
            Logger.Write("WebSocket not connected");
            return;
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes( JsonSerializer.Serialize(context) );
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                _cts.Token);
        }
        catch (Exception ex)
        {
            Logger.Write($"Error send: {ex.Message}");
            await ReconnectAsync();
        }
    }

    private async Task ReconnectAsync()
    {
        OnReconnecting?.Invoke();
        Logger.Write("Retry...");
        
        await CloseAsync(WebSocketCloseStatus.Empty, "Reconnecting");
        
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        await ConnectAsync();
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription)
    {
        try
        {
            if (_webSocket?.State == WebSocketState.Open || 
                _webSocket?.State == WebSocketState.CloseReceived)
            {
                await _webSocket.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            OnDisconnected?.Invoke();
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _webSocket?.Dispose();
        _cts?.Dispose();
    }
}