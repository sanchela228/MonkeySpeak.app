using System.Net;
using System.Net.Sockets;
using System.Text;

namespace App.System.Managers;

public class UdpHolePunchingManager
{
    private UdpClient _udpClient;
    private IPEndPoint _remoteEndPoint;
    private bool _isConnected = false;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _ownsClient = false;

    public event Action<byte[]> OnDataReceived;
    public event Action<IPEndPoint, IPEndPoint> OnConnected;

    public void StartHolePunching(IPEndPoint remoteEndPoint, int localPort = 0)
    {
        _ownsClient = true;
        UdpClient client = null;
        try
        {
            client = new UdpClient(localPort);
            ConfigureClient(client);
            StartWithClient(client, remoteEndPoint, new CancellationTokenSource());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting UDP client: {ex.Message}");
            if (_ownsClient)
            {
                client?.Close();
                _ownsClient = false;
            }
        }
    }

    public void StartWithClient(UdpClient client, IPEndPoint remoteEndPoint, CancellationTokenSource cts)
    {
        _ownsClient = false;
        _udpClient = client;
        _remoteEndPoint = remoteEndPoint;
        _cancellationTokenSource = cts ?? new CancellationTokenSource();

        try
        {
            ConfigureClient(_udpClient);

            Console.WriteLine($"UDP client started on port {((IPEndPoint)_udpClient.Client.LocalEndPoint).Port}");
            Console.WriteLine($"Target endpoint: {remoteEndPoint}");
            
            Task.Run(() => HolePunchingTask(_cancellationTokenSource.Token));
            Task.Run(() => ReceiveTask(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting UDP with existing client: {ex.Message}");
        }
    }

    private void ConfigureClient(UdpClient client)
    {
        client.Client.ReceiveTimeout = 1000;
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452; // 0x9800000C
            client.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0 }, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UDP] Unable to disable ICMP reset: {ex.Message}");
        }
    }

    private async Task HolePunchingTask(CancellationToken cancellationToken)
    {
        int attempt = 0;
        byte[] pingPacket = Encoding.UTF8.GetBytes("PING");
        
        while (!_isConnected && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                Console.WriteLine($"Hole punching attempt #{attempt} to {_remoteEndPoint}");
                
                await _udpClient.SendAsync(pingPacket, pingPacket.Length, _remoteEndPoint);
                
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in hole punching: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task ReceiveTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync();
                byte[] data = result.Buffer;
                
                Console.WriteLine($"Received {data.Length} bytes from {result.RemoteEndPoint}");

                if (!_isConnected && result.RemoteEndPoint.Equals(_remoteEndPoint))
                {
                    _isConnected = true;
                    Console.WriteLine("Hole punching successful! Connection established.");
                    
                    var local = (IPEndPoint)_udpClient.Client.LocalEndPoint;
                    OnConnected?.Invoke(local, _remoteEndPoint);
                }

                OnDataReceived?.Invoke(data);
                
                if (Encoding.UTF8.GetString(data) == "PING")
                {
                    byte[] pongPacket = Encoding.UTF8.GetBytes("PONG");
                    await _udpClient.SendAsync(pongPacket, pongPacket.Length, result.RemoteEndPoint);
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                continue;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                Console.WriteLine("[UDP] ConnectionReset received (ICMP Port Unreachable). Continuing...");
                continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving data: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public async Task SendData(byte[] data)
    {
        if (_udpClient != null && _remoteEndPoint != null)
        {
            await _udpClient.SendAsync(data, data.Length, _remoteEndPoint);
        }
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        
        if (_ownsClient)
        {
            _udpClient?.Close();
            _ownsClient = false;
        }
        
        Console.WriteLine("UDP client stopped");
    }
}