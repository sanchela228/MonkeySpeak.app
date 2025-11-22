using System.ComponentModel.DataAnnotations;

namespace Core.Database.Models;

public enum SessionStatus
{
    Active,
    Closed,
    Expired
}

public class Session
{
    public int Id { get; set; }
    public string ApplicationId { get; set; }
    public Guid WebSocketSessionId { get; set; } = Guid.NewGuid();
    
    [StringLength(6)]
    public string Code { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 6);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public string? Metadata { get; set; }
}