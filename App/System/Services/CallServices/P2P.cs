using System.Net;
using App.System.Modules;
using App.System.Utils;
using App.System.Models.Websocket.Messages.NoAuthCall;

namespace App.System.Services.CallServices;

public class P2P : ICallService
{
    public Network Network { get; private set; } = Context.Instance.Network;
    public async void Connect()
    {
        IPEndPoint ip = await GoogleSTUNServer.GetPublicIPAddress();
    }


    public event Action<string> OnSessionCreated;
    public async void CreateSession()
    {
        Network.WebSocketClient.MessageDispatcher.On<SessionCreated>(msg =>
        {
            OnSessionCreated?.Invoke(msg.Value);
        });
        
        Network.WebSocketClient.SendAsync(new CreateSession()
        {
            Value = "test",
        });
    }
}