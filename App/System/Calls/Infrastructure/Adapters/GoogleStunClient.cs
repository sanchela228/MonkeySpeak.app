using System.Net;
using System.Threading.Tasks;
using App.System.Calls.Infrastructure;
using App.System.Utils;

namespace App.System.Calls.Infrastructure.Adapters;

public class GoogleStunClient : IStunClient
{
    public async Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs = 5000)
    {
        return await GoogleSTUNServer.GetPublicIPAddress(localPort);
    }
}
