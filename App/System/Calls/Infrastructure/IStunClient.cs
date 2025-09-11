using System.Net;
using System.Threading.Tasks;

namespace App.System.Calls.Infrastructure;

public interface IStunClient
{
    Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs = 5000);
}
