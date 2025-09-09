using System.Net;
using App.System.Modules;
using App.System.Utils;
using App.System.Models.Websocket.Messages.NoAuthCall;

namespace App.System.Services.CallServices;

public class P2P : ICallService
{
    public async void Connect()
    {
        IPEndPoint ip = await GoogleSTUNServer.GetPublicIPAddress();
    }


    public event Action<string> OnSessionCreated;
    public async void CreateSession()
    {
        Context.Instance.Network.WebSocketClient.MessageDispatcher.On<SessionCreated>(msg =>
        {
            OnSessionCreated?.Invoke(msg.Value);
        });

        var message = new CreateSession()
        {
            Value = "test",
        };
        
        Context.Instance.Network.WebSocketClient.SendAsync(message);
    }
}