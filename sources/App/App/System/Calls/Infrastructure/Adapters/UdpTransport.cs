using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace App.System.Calls.Infrastructure.Adapters;

using App.System.Calls.Infrastructure;

public class UdpTransport : ITransport
{
    public event Action<ReadOnlyMemory<byte>>? OnData;

    private UdpClient? _client;
    private IPEndPoint? _remote;

    public UdpTransport()
    {
    }

    public Task InitializeAsync(UdpClient client, IPEndPoint remote)
    {
        _client = client;
        _remote = remote;
        
        return Task.CompletedTask;
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data)
    {
        if (_client == null || _remote == null) throw new InvalidOperationException("Transport is not initialized");
        await _client.SendAsync(data.ToArray(), data.Length, _remote);
    }

    public Task CloseAsync()
    {
        _client?.Close();
        _client = null;
        _remote = null;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }
}
