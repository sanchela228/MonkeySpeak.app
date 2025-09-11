using System.Net;
using System.Text;
using App.System.Managers;
using App.System.Modules;
using App.System.Utils;
using App.System.Models.Websocket.Messages.NoAuthCall;

namespace App.System.Services.CallServices;

public class P2P : ICallService
{
    private UdpHolePunchingManager _udpManager;
    private int _localUdpPort;
    
    public async Task Connect(string session)
    {
        HolePunchingWait();
        
        IPEndPoint ip = await GoogleSTUNServer.GetPublicIPAddress(_localUdpPort);
        
        Context.Instance.Network.WebSocketClient.MessageDispatcher.On<SessionCreated>(msg =>
        {
            OnSessionCreated?.Invoke(msg.Value);
        });
        
        Context.Instance.Network.WebSocketClient.SendAsync(new ConnectToSession()
        {
            Code = session,
            IpEndPoint = ip.ToString(),
            Value = session,
        });
    }

    public event Action<string> OnSessionConnected;
    public event Action<string> OnSessionCreated;
    public async Task CreateSession()
    {
        HolePunchingWait();
        
        IPEndPoint ip = await GoogleSTUNServer.GetPublicIPAddress(_localUdpPort);
      
        
        Context.Instance.Network.WebSocketClient.MessageDispatcher.On<SessionCreated>(msg =>
        {
            OnSessionCreated?.Invoke(msg.Value);
        });
        
        Context.Instance.Network.WebSocketClient.SendAsync(new CreateSession()
        {
            Value = "",
            IpEndPoint = ip.ToString()
        });
    }

    private async void HolePunchingWait()
    {
        if (_localUdpPort == 0)
        {
            var rnd = new Random();
            
            #if DEBUG
                _localUdpPort = 5000 + rnd.Next(1000);
            #else
                _localUdpPort = 40000 + rnd.Next(20000);
            #endif
            
            Console.WriteLine($"[P2P] Selected local UDP port: {_localUdpPort}");
        }
        
        Context.Instance.Network.WebSocketClient.MessageDispatcher.On<HolePunching>(msg =>
        {
            try
            {
                Console.WriteLine($"Received HolePunching message for endpoint: {msg.IpEndPoint}");
                
                var parts = msg.IpEndPoint.Split(':');
                if (parts.Length == 2)
                {
                    IPAddress ipAddress = IPAddress.Parse(parts[0]);
                    int port = int.Parse(parts[1]);
                    IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, port);
                    
                    _udpManager?.Stop();
                
                    _udpManager = new UdpHolePunchingManager();
                    _udpManager.OnDataReceived += HandleUdpData;
                    
                    _udpManager.StartHolePunching(remoteEndPoint, _localUdpPort);
            
                    Console.WriteLine($"Started hole punching to {remoteEndPoint}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERRRRRR: {e.Message}");
                throw;
            }
            
        });
    }
    
    private void HandleUdpData(byte[] data)
    {
        string message = Encoding.UTF8.GetString(data);
        Console.WriteLine($"Received UDP message: {message}");
    
        if (message == "PONG")
        {
            Console.WriteLine("Received PONG response!");
        }
    }
}