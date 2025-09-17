using System;
using System.Net;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace App.System.Calls.Infrastructure;

public interface IHolePuncher
{
    event Action<byte[]> OnData;
    event Action<IPEndPoint, IPEndPoint> OnConnected;
    Task StartAsync(IPEndPoint remote, int localPort, CancellationToken ct);
    Task StartWithClientAsync(UdpClient client, IPEndPoint remote, CancellationToken ct);
    Task SendAsync(byte[] data);
    Task StopAsync();
}