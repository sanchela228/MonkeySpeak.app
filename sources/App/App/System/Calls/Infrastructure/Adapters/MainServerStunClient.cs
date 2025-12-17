using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using App.System.Calls.Infrastructure;
using App.System.Services;
using App.System.Utils;

namespace App.System.Calls.Infrastructure.Adapters;

public class MainServerStunClient : IStunClient
{
    public async Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs = 5000, string domain = null)
    {
        return await GetPublicEndPointAsync(localPort, timeoutMs, new(), domain);
    }

    public async Task<IPEndPoint?> GetPublicEndPointAsync(int localPort, int timeoutMs, CancellationToken cancellationToken, string domain = "localhost")
    {
        using (var udp = new UdpClient(localPort))
        {
            try
            {
                udp.Connect(domain, 3478);

                byte[] data = Encoding.UTF8.GetBytes("ping");
                await udp.SendAsync(data, data.Length);

                using (var timeoutCts = new CancellationTokenSource(timeoutMs))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    var receiveResult = await udp.ReceiveAsync(linkedCts.Token);
                
                    string responseText = Encoding.UTF8.GetString(receiveResult.Buffer);
                    return IPEndPoint.Parse(responseText);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Write("UDP STUN request timed out or was cancelled");
                return null;
            }
            catch (SocketException ex)
            {
                Logger.Write($"Socket error: {ex.SocketErrorCode}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Write($"UDP STUN error: {ex.Message}");
                return null;
            }
        }
    }
}
