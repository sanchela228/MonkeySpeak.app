using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Configurations.Interfaces;
using App.System.Models.Websocket;
using App.System.Models.Websocket.Messages;

namespace App.System.Modules;

public class WebSocketClient(INetworkConfig conf)
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cts;
    private Uri _uri;
    private INetworkConfig _config = conf;

    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public async Task ConnectAsync()
    {
        _uri = new Uri($"ws://{conf.Domain}:{conf.Port}/connector");
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        try
        {
            Console.WriteLine("Пытаюсь подключиться...");
            await _webSocket.ConnectAsync(_uri, _cts.Token);
            Console.WriteLine($"Подключено! State: {_webSocket.State}");

            if (_webSocket.State == WebSocketState.Open)
            {
                OnConnected?.Invoke();
                
                _ = Task.Run(ReceiveMessagesAsync, _cts.Token);
                // _ = Task.Run(SendPingAsync, _cts.Token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения: {ex.Message}");
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
                    Console.WriteLine("Сервер запросил закрытие соединения");
                    await CloseAsync(WebSocketCloseStatus.NormalClosure, "Server requested close");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessageReceived?.Invoke(message);
                    Console.WriteLine($"Получено: {message}");
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                Console.WriteLine("Соединение разорвано сервером");
                await ReconnectAsync();
                break;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Операция получения отменена");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения: {ex.Message}");
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
            Console.WriteLine("WebSocket не подключен");
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
            Console.WriteLine($"Ошибка отправки: {ex.Message}");
            await ReconnectAsync();
        }
    }

    private async Task ReconnectAsync()
    {
        Console.WriteLine("Попытка переподключения...");
        
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