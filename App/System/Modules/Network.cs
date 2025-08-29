using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using App.Configurations.Interfaces;
using App.System.Services;
using App.System.Services.CallServices;
using Raylib_cs;

namespace App.System.Modules;


public class Network(INetworkConfig config) : IDisposable
{
    public enum NetworkState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }
    public INetworkConfig Config { get; set; } = config;
    public NetworkState State { get; set; } = NetworkState.Disconnected;
    
    public ICallService CallService
    {
        get => Config.CallService;
        protected set => Config.CallService = value;
    }
    
    public async Task ConnectServer()
    {   
        State = NetworkState.Connecting;
        Task.Run( CheckConnectionAsync );
    }
    
    
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;
    
    public event Action<string> OnMessageReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;
    
    private async Task CheckConnectionAsync()
    {
        await Task.Delay(400);
        var ping = await PingServer();
        int retries = 0;
        
        while (true)
        {
            if (Config.MaxRetries < retries)
            {
                State = NetworkState.Disconnected;
                break;
            }
            
            if (ping)
            {
                try
                {
                    Console.WriteLine("Пытаюсь подключиться...");
                    var client = new WebSocketClient(Config);
                    var messageDispatcher = new MessageDispatcher();
                    
                    client.OnMessageReceived += message => messageDispatcher.Configure(message);
                    client.OnConnected += () => State = NetworkState.Connected;
                    client.OnDisconnected += () => State = NetworkState.Disconnected;
                    client.OnReconnecting += () => State = NetworkState.Reconnecting;
                    
                    await client.ConnectAsync();
                }
                catch (Exception ex)
                {
                    State = NetworkState.Error;
                    Console.WriteLine($"Ошибка подключения: {ex.Message}");
                }
                
                break;
            }
            
            State = NetworkState.Reconnecting;
            await Task.Delay(1000);
            retries++;
                
            ping = await PingServer();
        }
    }
    
    private async Task<bool> PingServer()
    {
        try
        {
            using var httpClient = new HttpClient();
            {
                var response = await httpClient.GetAsync(MainUrl());
                return response.IsSuccessStatusCode;
            }
        }
        catch
        {
            State = NetworkState.Error;
            return false;
        }
    }

    public async Task<HttpResponseMessage> Get(string relativeUrl)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetAsync( GetUrl(relativeUrl) );
    }

    private string GetUrl(string relativeUrl)
    {
        string url = MainUrl();
        
        if (!relativeUrl.StartsWith("/"))
            relativeUrl = "/" + relativeUrl;

        url += relativeUrl;
        
        return url;
    }
    
    private string MainUrl()
    {
        string url = "";
        
        url += Config.UseSSL ? "https://" : "http://";
        url += Config.Domain + ":" + Config.Port;
        
        return url; 
    }

    public Color GetStateColor()
    {
        return State switch
        {
            NetworkState.Connected => Color.Green,
            NetworkState.Error => Color.Red,
            NetworkState.Connecting => Color.Yellow,
            NetworkState.Reconnecting => Color.Yellow,
            NetworkState.Disconnected => Color.Gray,
            _ => Color.Gray
        };
    }
    
    public void Dispose()
    {
        // throw new NotImplementedException();
    }
}