using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextDatabase = Core.Database.Context;

namespace Core.Websockets;

public class WebsocketMiddleware(WebSocket ws, ContextDatabase dbContext, ConcurrentDictionary<Guid, Connection> connections, 
    ConcurrentDictionary<string, Room> rooms)
{
    public MessageDispatcher MessageDispatcher { get; } = new();
    public async Task OpenWebsocketConnection(HttpContext context)
    {
        var connection = new Connection(ws);
        connections.TryAdd(connection.Id, connection);

        try
        {
            MessageDispatcher.Configure(dbContext, connections, rooms, this);
            await HandleWebSocket(connection);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Websocket connect failed: {e.Message}");
            throw;
        }
        finally
        {
            if (rooms.Count > 0)
            {
                var room = rooms.Values.FirstOrDefault(r => r.Connections.ContainsKey(connection.Id));
                if (room != null)
                {
                    room.Connections.TryRemove(connection.Id, out _);

                    if (room.Connections.Count == 0)
                    {
                        rooms.TryRemove(room.Code, out _);
                    }
                    else
                    {
                        foreach (var conn in room.Connections.Values)
                        {
                            conn.Send(new Messages.NoAuthCall.InterlocutorLeft
                            {
                                InterlocutorId = connection.Id.ToString(),
                                Value = "Participant left"
                            });
                        }

                        room.SetState(room.Connections.Count >= 2 ? Room.RoomState.Running : Room.RoomState.Waiting);
                    }
                }
            }

            connections.TryRemove(connection.Id, out _);
        }
    }

    private async Task HandleWebSocket(Connection connection)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (connection.WebSocket.State == WebSocketState.Open)
            {
                using var memoryStream = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await connection.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await connection.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null,
                            CancellationToken.None);
                        return;
                    }

                    memoryStream.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(memoryStream.ToArray());
                Console.WriteLine($"Receive from {connection.Id}: {message}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {
                    var messageObj = JsonSerializer.Deserialize<Context>(message, options);

                    if (messageObj != null)
                        MessageDispatcher.Dispatch(messageObj, connection);
                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine($"Error handling message: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket connection closed unexpectedly for {connection.Id}: {ex.Message}");
        }
    }
    
    public async Task SendMessage(IMessage message, Connection conn)
    {
        var context = Context.Create(message);
        var buffer = Encoding.UTF8.GetBytes( JsonSerializer.Serialize<Context>(context) );
        
        await conn.WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}