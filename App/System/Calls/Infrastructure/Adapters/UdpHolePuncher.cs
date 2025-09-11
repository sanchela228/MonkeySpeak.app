using System;
using System.Net;
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

    public Task StartAsync(IPEndPoint remote, int localPort, CancellationToken ct)
    {
        _manager = new UdpHolePunchingManager();
        _manager.OnDataReceived += data => OnData?.Invoke(data);

        _ctr = ct.Register(() => _manager.Stop());

        _manager.StartHolePunching(remote, localPort);
        return Task.CompletedTask;
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
}
