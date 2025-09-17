using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using App.System.Calls.Infrastructure;
using App.System.Managers;

namespace App.System.Calls.Infrastructure.Adapters;

public class UdpHolePuncher : IHolePuncher
{
    private UdpHolePunchingManager _manager;
    private CancellationTokenRegistration _ctr;

    public event Action<byte[]> OnData;
    public event Action<IPEndPoint, IPEndPoint> OnConnected;

    public Task StartAsync(IPEndPoint remote, int localPort, CancellationToken ct)
    {
        var client = new UdpClient(localPort);
        return StartWithClientAsync(client, remote, ct);
    }

    public async Task SendAsync(byte[] data)
    {
        if (_manager != null)
        {
            await _manager.SendData(data);
        }
    }

    public Task StopAsync()
    {
        _manager?.Stop();
        _ctr.Dispose();
        return Task.CompletedTask;
    }

    public Task StartWithClientAsync(UdpClient client, IPEndPoint remote, CancellationToken ct)
    {
        _manager = new UdpHolePunchingManager();
        _manager.OnDataReceived += data => OnData?.Invoke(data);
        _manager.OnConnected += (local, remoteEp) => OnConnected?.Invoke(local, remoteEp);

        _ctr = ct.Register(() => _manager.Stop());

        var cts = new CancellationTokenSource();
        _manager.StartWithClient(client, remote, cts);
        return Task.CompletedTask;
    }
}
