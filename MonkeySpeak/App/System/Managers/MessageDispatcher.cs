using App.System.Models.Websocket;
using App.System.Services;

namespace App.System.Managers;

public class MessageDispatcher
{
    private readonly Dictionary<Type, Action<IMessage>> _handlers = new();

    public void On<T>(Action<T> handler) where T : IMessage
    {
        _handlers[typeof(T)] = (msg) => handler((T)msg);
    }

    public void Dispatch(App.System.Models.Websocket.Context context)
    {
        var message = context.ToMessage();
        var type = message.GetType();

        if (_handlers.TryGetValue(type, out var handler))
            handler(message);
    }

    public void Configure(App.System.Models.Websocket.Context context)
    {
        On<Models.Websocket.Messages.Ping>(msg =>
        {
            Logger.Write($"[MessageDispatcher] ping {msg.Value}");
        });
        
        On<Models.Websocket.Messages.ReturnedAuthToken>(msg =>
        {
            Logger.Write($"[MessageDispatcher] ReturnedAuthToken: {msg.Value}");
        });
                        
        Dispatch(context);
    }
}