using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Database.Models;

public class Friendship
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; }
    
    [Required]
    public Guid FriendId { get; set; }
    
    [ForeignKey(nameof(FriendId))]
    public virtual User Friend { get; set; }
    
    [Required]
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? AcceptedAt { get; set; }
}
