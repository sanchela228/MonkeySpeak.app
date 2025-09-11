using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace App.System.Calls.Infrastructure;

public interface IHolePuncher
{
    event Action<byte[]> OnData;
    Task StartAsync(IPEndPoint remote, int localPort, CancellationToken ct);
    Task SendAsync(byte[] data);
    Task StopAsync();
}
