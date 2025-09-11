using System;
using System.Threading.Tasks;
using App.System.Models.Websocket;
using App.System.Models.Websocket.Messages;

namespace App.System.Calls.Infrastructure;

public interface ISignalingClient
{
    event Action<Models.Websocket.Context> OnMessage;
    Task ConnectAsync();
    Task SendAsync(IMessage message);
}
