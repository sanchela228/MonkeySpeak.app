using System.Net;
using System.Net.Sockets;
using App.System.Services;

namespace App.System.Calls.Media;

public class AudioTranslator
{
    private UdpClient _udpClient;
    private IPEndPoint _remoteEndPoint;
    private CancellationTokenSource _cancellationTokenSource;
    
    public AudioTranslator(UdpClient client, IPEndPoint remoteEndPoint, CancellationTokenSource cts)
    {
        _udpClient = client;
        _remoteEndPoint = remoteEndPoint;
        _cancellationTokenSource = cts ?? new CancellationTokenSource();

        try
        {
            ConfigureClient(_udpClient);
            
            Task.Run(() => ReceiveTask(_cancellationTokenSource.Token));

            Logger.Write(Logger.Type.Info, $"[AudioTranslator] Client started on {_udpClient.Client.LocalEndPoint}");
            Logger.Write(Logger.Type.Info, $"[AudioTranslator] Target endpoint: {remoteEndPoint}");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error starting UDP with existing client: {ex.Message}", ex);
        }
    }
    
    public event Action<byte[]> OnDataReceived;
    
    private async Task ReceiveTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync();
                byte[] data = result.Buffer;
                
                Console.WriteLine("[AudioTranslator] ReceiveTask", data);
                
                OnDataReceived?.Invoke(data);
                
            }
            catch (Exception ex)
            {
                Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error receiving data: {ex.Message}", ex);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public async void SendAudioBytes(byte[] data, int length)
    {
        Console.WriteLine("SendAudioBytes: " + _remoteEndPoint);
        await _udpClient.SendAsync(data, length, _remoteEndPoint);
    }
    
    
    
    
    
    
    private void ConfigureClient(UdpClient client)
    {
        client.Client.ReceiveTimeout = 1000;
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452; // 0x9800000C
            client.Client.IOControl((IOControlCode) SIO_UDP_CONNRESET, new byte[] { 0 }, null);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Unable to disable ICMP reset: {ex.Message}", ex);
        }
    }
}