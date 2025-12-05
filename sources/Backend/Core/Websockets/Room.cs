using System.Collections.Concurrent;
using System.Text;

namespace Core.Websockets;

public class Room
{
    public readonly Guid Id;
    public readonly string Code;
    public ConcurrentDictionary<Guid, Connection> Connections { get; } = new();

    public RoomState State { get; private set; } = RoomState.Waiting;
    
    private Connection _creator;

    public Room(Connection creator)
    {
        Id = Guid.NewGuid();
        
        // TODO: CLEAR THIS
        
        // const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        const string chars = "asdwfx";
        var random = new Random();
        var strR = new StringBuilder(6);
    
        for (int i = 0; i < 6; i++)
        {
            strR.Append(chars[random.Next(chars.Length)]);
        }
        
        Code = strR.ToString();
        Connections.TryAdd(creator.Id, creator);
        
        _creator = creator;
    }

    public void SetState(RoomState state)
    {
        State = state;
        OnStateChange?.Invoke(this, state);
    }

    public event Action<Room, RoomState>? OnStateChange;
    
    public bool IsCreator(Connection conn) => _creator == conn;

    public enum RoomState
    {
        Waiting,
        Running
    }
}