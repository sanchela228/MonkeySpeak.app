using System.Net;
using System.Threading.Tasks;
using App.System.Calls.Infrastructure;
using App.System.Utils;

namespace App.System.Calls.Infrastructure.Adapters;

public class GoogleStunClient : IStunClient
{
    public async Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs = 5000, string domain = null)
    {
        return await GoogleSTUNServer.GetPublicIPAddress(localPort);
    }

    public async Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs, CancellationToken cancellationToken, string domain = null)
    {
        return await GoogleSTUNServer.GetPublicIPAddress(localPort, cancellationToken: cancellationToken);
    }
}
