using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    public event Action<string, IPEndPoint, IPEndPoint>? OnInterlocutorConnected;
    public event Action<string, byte[]>? OnAudioDataByInterlocutor;
    public event Action<string, byte[]>? OnControlDataByInterlocutor;
    public event Action<string, byte[]>? OnHolePunchDataByInterlocutor;

    private UdpClient? _client;
    private IPEndPoint? _remote;
    private CancellationTokenSource? _cts;
    private volatile bool _isConnected;

    // Multi-interlocutor state
    private readonly Dictionary<string, IPEndPoint> _interlocutorToRemote = new();
    private readonly Dictionary<IPEndPoint, string> _remoteToInterlocutor = new();
    private readonly Dictionary<string, bool> _interlocutorConnected = new();
    private readonly Dictionary<string, CancellationTokenSource> _interlocutorCts = new();

    public void StartWithClient(UdpClient client, IPEndPoint remote, CancellationToken cancellationToken)
    {
        _client = client;
        _remote = remote;
        ConfigureClient(_client);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Task.Run(() => ReceiveLoop(_cts.Token));
        Task.Run(() => HolePunchLoop(_cts.Token));
    }

    public void AddInterlocutor(string interlocutorId, IPEndPoint remote)
    {
        if (_client == null) throw new InvalidOperationException("UDP not initialized");
        Console.WriteLine($"[UDP] AddInterlocutor: {interlocutorId.Substring(0, Math.Min(8, interlocutorId.Length))} -> {remote}");
        _interlocutorToRemote[interlocutorId] = remote;
        _remoteToInterlocutor[remote] = interlocutorId;
        Console.WriteLine($"[UDP] Mapping created. Total interlocutors: {_interlocutorToRemote.Count}");
        _interlocutorConnected[interlocutorId] = _interlocutorConnected.TryGetValue(interlocutorId, out var c) && c;

        if (_cts != null)
        {
            if (_interlocutorCts.TryGetValue(interlocutorId, out var oldCts))
            {
                try { oldCts.Cancel(); oldCts.Dispose(); } catch { }
            }
            var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            _interlocutorCts[interlocutorId] = linked;
            _ = Task.Run(() => HolePunchLoopForInterlocutor(interlocutorId, remote, linked.Token));
        }
    }

    public void RemoveInterlocutor(string interlocutorId)
    {
        if (_interlocutorCts.TryGetValue(interlocutorId, out var cts))
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
            _interlocutorCts.Remove(interlocutorId);
        }
        if (_interlocutorToRemote.TryGetValue(interlocutorId, out var ep))
        {
            _remoteToInterlocutor.Remove(ep);
        }
        _interlocutorToRemote.Remove(interlocutorId);
        _interlocutorConnected.Remove(interlocutorId);
    }

    private async Task HolePunchLoopForInterlocutor(string interlocutorId, IPEndPoint remote, CancellationToken ct)
    {
        int attempt = 0;
        while (!_interlocutorConnected.GetValueOrDefault(interlocutorId) && !ct.IsCancellationRequested)
        {
            try
            {
                attempt++;
                var payload = Encoding.UTF8.GetBytes("PING");
                await SendToRemoteAsync(MessageType.HolePunch, payload, remote);
                await Task.Delay(100, ct);
            }
            catch
            {
                await Task.Delay(500, ct);
            }
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
                        // Backward-compat single-remote path
                        if (!_isConnected && _remote != null && result.RemoteEndPoint.Equals(_remote))
                        {
                            _isConnected = true;
                            OnConnected?.Invoke((IPEndPoint)_client.Client.LocalEndPoint!, _remote);
                        }

                        // Multi-interlocutor path
                        if (_remoteToInterlocutor.TryGetValue(result.RemoteEndPoint, out var ilId))
                        {
                            if (!_interlocutorConnected.GetValueOrDefault(ilId))
                            {
                                _interlocutorConnected[ilId] = true;
                                OnInterlocutorConnected?.Invoke(ilId, (IPEndPoint)_client.Client.LocalEndPoint!, result.RemoteEndPoint);
                            }
                        }
                        else
                        {
                            foreach (var kvp in _interlocutorToRemote)
                            {
                                if (kvp.Value.Address.Equals(result.RemoteEndPoint.Address))
                                {
                                    var oldRemote = kvp.Value;
                                    _remoteToInterlocutor.Remove(oldRemote, out _);
                                    _interlocutorToRemote[kvp.Key] = result.RemoteEndPoint;
                                    _remoteToInterlocutor[result.RemoteEndPoint] = kvp.Key;
                                    
                                    if (!_interlocutorConnected.GetValueOrDefault(kvp.Key))
                                    {
                                        _interlocutorConnected[kvp.Key] = true;
                                        OnInterlocutorConnected?.Invoke(kvp.Key, (IPEndPoint)_client.Client.LocalEndPoint!, result.RemoteEndPoint);
                                    }
                                    break;
                                }
                            }
                        }

                        var text = Encoding.UTF8.GetString(payload);
                        if (text == "PING")
                        {
                            var pong = Encoding.UTF8.GetBytes("PONG");
                            if (_remoteToInterlocutor.TryGetValue(result.RemoteEndPoint, out var ilId2) && _interlocutorToRemote.TryGetValue(ilId2, out var ep))
                                await SendToRemoteAsync(MessageType.HolePunch, pong, ep);
                            else
                                await SendAsync(MessageType.HolePunch, pong);
                        }
                        OnHolePunchData?.Invoke(payload);
                        if (_remoteToInterlocutor.TryGetValue(result.RemoteEndPoint, out var ilId3))
                            OnHolePunchDataByInterlocutor?.Invoke(ilId3, payload);
                        break;
                    
                    case MessageType.Audio:
                        OnAudioData?.Invoke(payload);
                        if (_remoteToInterlocutor.TryGetValue(result.RemoteEndPoint, out var ilIdA))
                        {
                            OnAudioDataByInterlocutor?.Invoke(ilIdA, payload);
                        }
                        else
                        {
                            Console.WriteLine($"[UDP] Audio from unknown endpoint: {result.RemoteEndPoint}. Known interlocutors: {_remoteToInterlocutor.Count}");
                        }
                        break;
                    
                    case MessageType.Control:
                        OnControlData?.Invoke(payload);
                        if (_remoteToInterlocutor.TryGetValue(result.RemoteEndPoint, out var ilIdC))
                            OnControlDataByInterlocutor?.Invoke(ilIdC, payload);
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

    private async Task SendToRemoteAsync(MessageType type, ReadOnlyMemory<byte> payload, IPEndPoint remote)
    {
        if (_client == null) throw new InvalidOperationException("UDP not initialized");
        var buf = new byte[1 + payload.Length];
        buf[0] = (byte)type;
        payload.CopyTo(buf.AsMemory(1));
        await _client.SendAsync(buf, buf.Length, remote);
    }

    public async Task SendAsync(MessageType type, ReadOnlyMemory<byte> payload)
    {
        if (_client == null) throw new InvalidOperationException("UDP not initialized");

        if (_interlocutorToRemote.Count > 0)
        {
            foreach (var kv in _interlocutorToRemote)
            {
                await SendToRemoteAsync(type, payload, kv.Value);
            }
        }
        else if (_remote != null)
        {
            await SendToRemoteAsync(type, payload, _remote);
        }
        else
        {
            throw new InvalidOperationException("UDP remote(s) not initialized");
        }
    }

    public Task SendAudioAsync(ReadOnlyMemory<byte> payload) => SendAsync(MessageType.Audio, payload);
    public Task SendControlAsync(ReadOnlyMemory<byte> payload) => SendAsync(MessageType.Control, payload);
    public Task SendControlToInterlocutorAsync(string interlocutorId, ReadOnlyMemory<byte> payload)
    {
        if (!_interlocutorToRemote.TryGetValue(interlocutorId, out var ep))
            throw new InvalidOperationException("Unknown interlocutor");
        return SendToRemoteAsync(MessageType.Control, payload, ep);
    }

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
