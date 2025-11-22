using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using App.System.Calls.Infrastructure;
using App.System.Services;
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
        Logger.Write("[STUN:DemoStunClientWithHttp] GetPublicEndPointAsync_method - Start");
        
        try
        {
            _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
            
            int portFromConfig = Context.Network.Config.Port;
            var response = await _httpClient.GetStringAsync($"http://{domain}:{portFromConfig}/get-public-endpoint");
            response = response.Trim();
            
            Logger.Write($"[STUN:DemoStunClientWithHttp] origin response - {response}");
            
            if (response.StartsWith("["))
            {
                int endBracket = response.IndexOf(']');
                if (endBracket > 0 && endBracket + 2 < response.Length)
                {
                    string ipPart = response.Substring(1, endBracket - 1);
                    string portPart = response.Substring(endBracket + 2);
                    if (IPAddress.TryParse(ipPart, out var ipv1) && int.TryParse(portPart, out var portv1))
                        return new IPEndPoint(ipv1, portv1);
                }
            }
            
            int lastColon = response.LastIndexOf(':');
            if (lastColon > 0)
            {
                string ipPart = response.Substring(0, lastColon);
                string portPart = response.Substring(lastColon + 1);

                if (ipPart.StartsWith("::ffff:"))
                    ipPart = ipPart.Replace("::ffff:", "");

                if (IPAddress.TryParse(ipPart, out var ipv2) && int.TryParse(portPart, out var portv2))
                    return new IPEndPoint(ipv2, portv2);
            }
            
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
            Logger.Write($"[STUN:DemoStunClientWithHttp] error: {ex.Message}", Logger.Type.Error);
            return null;
        }
    }
}
