using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace App.System.Managers;

public class UdpUnifiedManager : IDisposable
{
    public enum MessageType : byte
    {
        HolePunch,
        Audio,
        Control
    }

    public event Action<IPEndPoint, IPEndPoint>? OnConnected;
    public event Action<byte[]>? OnAudioData;
    public event Action<byte[]>? OnControlData;
    public event Action<byte[]>? OnHolePunchData;

    private UdpClient? _client;
    private IPEndPoint? _remote;
    private CancellationTokenSource? _cts;
    private volatile bool _isConnected;

    public void StartWithClient(UdpClient client, IPEndPoint remote, CancellationToken cancellationToken)
    {
        _client = client;
        _remote = remote;
        ConfigureClient(_client);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task.Run(() => ReceiveLoop(_cts.Token));
        Task.Run(() => HolePunchLoop(_cts.Token));
    }

    private void ConfigureClient(UdpClient client)
    {
        client.Client.ReceiveTimeout = 1000;
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452; // 0x9800000C
            client.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0 }, null);
        }
        catch
        {
        }
    }

    private async Task HolePunchLoop(CancellationToken ct)
    {
        int attempt = 0;
        while (!_isConnected && !ct.IsCancellationRequested)
        {
            try
            {
                attempt++;
                var payload = Encoding.UTF8.GetBytes("PING");
                await SendAsync(MessageType.HolePunch, payload);
                await Task.Delay(100, ct);
            }
            catch
            {
                await Task.Delay(500, ct);
            }
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        if (_client == null) return;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _client.ReceiveAsync();
                var data = result.Buffer;
                if (data.Length == 0) continue;

                var type = (MessageType)data[0];
                var payload = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, payload, 0, payload.Length);

                switch (type)
                {
                    case MessageType.HolePunch:
                        if (!_isConnected && _remote != null && result.RemoteEndPoint.Equals(_remote))
                        {
                            _isConnected = true;
                            OnConnected?.Invoke((IPEndPoint)_client.Client.LocalEndPoint!, _remote);
                        }
                        
                        var text = Encoding.UTF8.GetString(payload);
                        if (text == "PING")
                        {
                            var pong = Encoding.UTF8.GetBytes("PONG");
                            await SendAsync(MessageType.HolePunch, pong);
                        }
                        OnHolePunchData?.Invoke(payload);
                        break;
                    case MessageType.Audio:
                        OnAudioData?.Invoke(payload);
                        break;
                    case MessageType.Control:
                        OnControlData?.Invoke(payload);
                        break;
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // ---
            }
            catch
            {
                await Task.Delay(200, ct);
            }
        }
    }

    public async Task SendAsync(MessageType type, ReadOnlyMemory<byte> payload)
    {
        if (_client == null || _remote == null) throw new InvalidOperationException("UDP not initialized");
        var buf = new byte[1 + payload.Length];
        buf[0] = (byte)type;
        payload.CopyTo(buf.AsMemory(1));
        await _client.SendAsync(buf, buf.Length, _remote);
    }

    public Task SendAudioAsync(ReadOnlyMemory<byte> payload) => SendAsync(MessageType.Audio, payload);
    public Task SendControlAsync(ReadOnlyMemory<byte> payload) => SendAsync(MessageType.Control, payload);

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
    }

    public void Dispose()
    {
        Stop();
        _client?.Dispose();
        _client = null;
    }
}
