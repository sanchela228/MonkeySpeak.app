namespace App.System.Models.Websocket.Messages.AuthCall;

public class FriendInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string LastSeenAt { get; set; } = string.Empty;
}

public class FriendListResponse : IMessage
{
    public List<FriendInfo> Friends { get; set; } = new();
    public string Value { get; set; } = string.Empty;
}
