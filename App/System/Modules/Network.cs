using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using App.Configurations;
using App.System.Services.CallServices;

namespace App.System.Modules;


public class Network : IDisposable
{
    public enum NetworkState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }
    public INetworkConfig Config { get; set; }
    public NetworkState State { get; set; } = NetworkState.Disconnected;
    
    public ICallService CallService
    {
        get => Config.CallService;
        protected set => Config.CallService = value;
    }

    public Network(INetworkConfig config)
    {
        Config = config;
    }

    public async Task<HttpResponseMessage> Get(string relativeUrl)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetAsync( GetUrl(relativeUrl) );
    }

    private string GetUrl(string relativeUrl)
    {
        string url = "";
        
        url += Config.UseSSL ? "https://" : "http://";
        url += Config.Domain + ":" + Config.Port;

        if (!relativeUrl.StartsWith("/"))
            relativeUrl = "/" + relativeUrl;

        url += relativeUrl;
        
        return url;
    }
    
    public void Dispose()
    {
        // throw new NotImplementedException();
    }
}