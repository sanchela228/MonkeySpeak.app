using System.Net.WebSockets;
using App.Configurations.Interfaces;
using App.System.Managers;
using App.System.Services;
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
    public WebSocketClient WebSocketClient { get; private set; }
    
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

    public event Action OnServerConnected; 
    public async Task ConnectServer()
    {   
        State = NetworkState.Connecting;
        Task.Run( CheckConnectionAsync );
    }
    
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;
    public Updater.DownloadUpdateState DownloadUpdateState;

    public async Task<bool> ConnectToNoAuthCallSession(string code)
    {
        
        Task.Delay(500);
        return false;
    }
    
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
                    Logger.Write(Logger.Type.Info, "WebSocket connection...");   
                    
                    WebSocketClient = new WebSocketClient(Config);
                    var updater = new Updater(Config);
                    
                    WebSocketClient.OnMessageReceived += message => WebSocketClient.MessageDispatcher.Configure(message);
                    WebSocketClient.OnConnected += async () =>
                    {
                        if ( await updater.CheckUpdate() )
                            await updater.StartProcessUpdate();
                        
                        State = NetworkState.Connected;
                        OnServerConnected?.Invoke();
                    };
                    WebSocketClient.OnDisconnected += () => State = NetworkState.Disconnected;
                    WebSocketClient.OnReconnecting += () => State = NetworkState.Reconnecting;
                    
                    await WebSocketClient.ConnectAsync();
                }
                catch (Exception ex)
                {
                    State = NetworkState.Error;
                    Logger.Write(Logger.Type.Error, "WebSocket connection error", ex);   
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
        
        var secureStorage = Context.SecureStorage;
        
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
    
    public void Dispose()
    {
        // throw new NotImplementedException();
    }
}