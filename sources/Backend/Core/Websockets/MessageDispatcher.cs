using System;
using System.Collections.Concurrent;
using System.Linq;
using ContextDatabase = Core.Database.Context;

namespace Core.Websockets;

public class MessageDispatcher
{
    private readonly System.Collections.Generic.Dictionary<Type, Action<IMessage, Connection>> _handlers = new();

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

    public void Configure(ContextDatabase dbContext, ConcurrentDictionary<Guid, Connection> connections, 
        ConcurrentDictionary<string, Room> rooms, WebsocketMiddleware middleware)
    {
        On<Messages.Ping>((msg, author) =>
        {
            Console.WriteLine($"Получен Ping: {msg.Value}");
        });

        On<Messages.NoAuthCall.CreateSession>((msg, author) =>
        {
            author.PublicIp = msg.IpEndPoint;

            var room = rooms.Values.FirstOrDefault(r => r.IsCreator(author) && r.State == Room.RoomState.Waiting);
            if (room == null)
            {
                room = new Room(author);
                rooms.TryAdd(room.Code, room);
            }

            author.Status = Connection.StatusConnection.Connected;
            author.Send(new Messages.NoAuthCall.SessionCreated
            {
                Value = room.Code,
                SelfInterlocutorId = author.Id.ToString()
            });
        });

        On<Messages.NoAuthCall.ConnectToSession>((msg, author) =>
        {
            if (string.IsNullOrWhiteSpace(msg.Code) || string.IsNullOrWhiteSpace(msg.IpEndPoint))
            {
                author.Send(new Messages.NoAuthCall.ErrorConnectToSession { Value = "Invalid parameters" });
                return;
            }

            author.PublicIp = msg.IpEndPoint;

            if (!rooms.TryGetValue(msg.Code, out var room) || room.Connections.Count <= 0)
            {
                author.Send(new Messages.NoAuthCall.ErrorConnectToSession { Value = "" });
                return;
            }

            room.Connections.TryAdd(author.Id, author);

            room.SetState(room.Connections.Count >= 2 ? Room.RoomState.Running : Room.RoomState.Waiting);
            author.Status = Connection.StatusConnection.Connecting;

            foreach (var conn in room.Connections.Values.Where(c => c != author))
            {
                conn.Send(new Messages.NoAuthCall.InterlocutorJoined
                {
                    IpEndPoint = author.PublicIp,
                    Value = room.Code,
                    Id = author.Id.ToString()
                });

                author.Send(new Messages.NoAuthCall.InterlocutorJoined
                {
                    IpEndPoint = conn.PublicIp,
                    Value = room.Code,
                    Id = conn.Id.ToString()
                });
            }
        });
        
        On<Messages.NoAuthCall.SuccessConnectedSession>((msg, author) =>
        {
            author.Status = Connection.StatusConnection.Connected;
        });
        
        On<Messages.NoAuthCall.HangupSession>((msg, author) =>
        {
            author.Status = Connection.StatusConnection.Idle;

            var room = rooms.Values.FirstOrDefault(x => x.Connections.ContainsKey(author.Id));
            if (room == null) return;

            room.Connections.TryRemove(author.Id, out _);

            foreach (var conn in room.Connections.Values)
            {
                conn.Send(new Messages.NoAuthCall.InterlocutorLeft
                {
                    Value = "Participant left",
                    InterlocutorId = author.Id.ToString()
                });
            }

            if (room.Connections.Count == 0)
                rooms.TryRemove(room.Code, out _);
            else if (room.Connections.Count == 1)
                room.SetState(Room.RoomState.Waiting);
        });
    }
}