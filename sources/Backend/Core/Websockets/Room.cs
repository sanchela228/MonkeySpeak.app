using System.Text;

namespace Core.Websockets;

public class Room
{
    public readonly Guid Id;
    public readonly string Code;
    public readonly List<Connection> Connections = [];

    public RoomState State { get; private set; } = RoomState.Waiting;
    
    private Connection _creator;

    public Room(Connection creator)
    {
        Id = Guid.NewGuid();
        
        // TODO: CLEAR THIS
        
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var strR = new StringBuilder(6);
    
        for (int i = 0; i < 6; i++)
        {
            strR.Append(chars[random.Next(chars.Length)]);
        }
        
        Code = strR.ToString();
        Connections.Add(creator);
        
        _creator = creator;
    }
    
    public bool IsCreator(Connection conn) => _creator == conn;

    public enum RoomState
    {
        Waiting,
        Running
    }
}