using System.Net;
using System.Threading;

namespace App.System.Calls.Infrastructure;

public interface IStunClient
{
    Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs = 5000);
    Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs, CancellationToken cancellationToken);
}
