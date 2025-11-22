using ContextDatabase = Core.Database.Context;

namespace Core.Websockets;

public class MessageDispatcher
{
    private readonly Dictionary<Type, Action<IMessage, Connection>> _handlers = new();

    public void On<T>(Action<T, Connection> handler) where T : IMessage
    {
        _handlers[typeof(T)] = (msg, author) => handler((T)msg, author);
    }

    public void Dispatch(Context context, Connection author)
    {
        var message = context.ToMessage();
        var type = message.GetType();

        if (_handlers.TryGetValue(type, out var handler))
            handler(message, author);
    }

    public void Configure(ContextDatabase dbContext, List<Connection> connections, 
        List<Room> rooms, WebsocketMiddleware middleware)
    {
        On<Messages.Ping>((msg, author) =>
        {
            Console.WriteLine($"Получен Ping: {msg.Value}");
        });

        On<Messages.NoAuthCall.CreateSession>((msg, author) =>
        {
            author.PublicIp = msg.IpEndPoint;
            
            var room = rooms.FirstOrDefault(room => room.IsCreator(author) && room.State == Room.RoomState.Waiting);

            if (room != null)
            {
                author.Status = Connection.StatusConnection.Connected;
                author.Send(new Messages.NoAuthCall.SessionCreated()
                {
                    Value = room.Code
                });
            }
            else
            {
                room = new Room(author);
                rooms.Add(room);
            
                author.Status = Connection.StatusConnection.Connected;
                author.Send(new Messages.NoAuthCall.SessionCreated()
                {
                    Value = room.Code
                });
            }
        });

        On<Messages.NoAuthCall.ConnectToSession>((msg, author) =>
        {
            author.PublicIp = msg.IpEndPoint;
            
            var room = rooms.FirstOrDefault(room => room.Code == msg.Code);

            if (room == null || room.Connections.Count <= 0)
            {
                author.Send(new Messages.NoAuthCall.ErrorConnectToSession()
                {
                    Value = ""
                });
                
                return;
            }
            
            author.Status = Connection.StatusConnection.Connecting;
                
            connections.ForEach(conn =>
            {
                if ( !author.PublicIp.Equals(conn.PublicIp) )
                {
                    conn.Send(new Messages.NoAuthCall.HolePunching()
                    {
                        IpEndPoint = author.PublicIp,
                        Value = room.Code
                    });
                        
                    author.Send(new Messages.NoAuthCall.HolePunching()
                    {
                        IpEndPoint = conn.PublicIp,
                        Value = room.Code
                    });
                }
            });
            
            room.Connections.Add(author);
        });
        
        On<Messages.NoAuthCall.SuccessConnectedSession>((msg, author) =>
        {
            author.Status = Connection.StatusConnection.Connected;
        });
    }
}