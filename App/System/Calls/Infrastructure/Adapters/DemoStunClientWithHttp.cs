using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using App.System.Calls.Infrastructure;
using App.System.Utils;

namespace App.System.Calls.Infrastructure.Adapters;

public class DemoStunClientWithHttp : IStunClient
{
    private readonly HttpClient _httpClient = new HttpClient();

    public async Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs = 5000, string domain = null)
    {
        return await GetPublicEndPointAsync(localPort, timeoutMs, new CancellationToken(), domain);
    }

    public async Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs, CancellationToken cancellationToken, string domain = null)
    {
        try
        {
            _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
            
            int portFromConfig = Context.Instance.Network.Config.Port;
            var response = await _httpClient.GetStringAsync($"http://{domain}:{portFromConfig}/get-public-endpoint");
            
            var parts = response.Split(':');

            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ip) && int.TryParse(parts[1], out var port))
            {
                return new IPEndPoint(ip, port);
            }
            else if (parts.Length == 1 && IPAddress.TryParse(parts[0], out var ipOnly))
            {
                return new IPEndPoint(ipOnly, localPort);
            }
            else
            {
                return new IPEndPoint(IPAddress.Loopback, localPort);
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HTTP STUN error: {ex.Message}");
            return null;
        }
    }
}
