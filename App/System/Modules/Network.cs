using System.Net.WebSockets;
using App.Configurations.Interfaces;
using App.System.Managers;
using App.System.Utils;
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
    private NetworkState _state = NetworkState.Disconnected;
    
    private HttpClient _httpClient = new HttpClient();
    public NetworkState State 
    { 
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnStateChanged?.Invoke(this, value);
            }
        }
    }
    
    public event EventHandler<NetworkState> OnStateChanged;
    
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
    public Updater.DownloadUpdateState downloadUpdateState;
    
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
                    var updater = new Updater(Config);
                    var messageDispatcher = new MessageDispatcher();
                    
                    client.OnMessageReceived += message => messageDispatcher.Configure(message);
                    client.OnConnected += async () =>
                    {
                        if ( await updater.CheckUpdate() )
                            await updater.StartProcessUpdate(downloadUpdateState);
                        
                        State = NetworkState.Connected;
                    };
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
            await Task.Delay(1);
            retries++;
                
            ping = await PingServer();
        }
    }
    
    private async Task<bool> PingServer()
    {
        try
        {
            var response = await _httpClient.GetAsync(MainUrl());
            return response.IsSuccessStatusCode;
        }
        catch
        {
            State = NetworkState.Error;
            return false;
        }
    }

    public async Task<HttpResponseMessage> Get(string relativeUrl) => await _httpClient.GetAsync(GetUrl(relativeUrl));
    
    public string GenerateAuthorizationUrl()
    {
        var url = GetUrl("/auth");
        
        var secureStorage = Context.Instance.SecureStorage;
        
        string codeVerifier = PkceHelper.GenerateCodeVerifier();
        secureStorage.Save("temp_code_verifier", codeVerifier);
        
        string codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
        string publicKey = secureStorage.Load("device_public_key");
        
        string websocketSession = "test";
        url += $"?client_id=desktop_app_monkeyspeak&" +
               $"code_challenge={codeChallenge}&" +
               $"websocket_session={websocketSession}&" +
               $"code_challenge_method=S256&" +
               $"device_public_key={Uri.EscapeDataString(publicKey)}&";
        
        return url;
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