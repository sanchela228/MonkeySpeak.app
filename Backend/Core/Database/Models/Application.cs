namespace Core.Database.Models;

public class Application
{
    public int Id { get; set; }
    public string ApplicationId { get; set; }
    public DateTime LastVisitAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}