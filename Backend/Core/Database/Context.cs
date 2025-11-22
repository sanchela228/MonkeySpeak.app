using Microsoft.EntityFrameworkCore;

namespace Core.Database;

public class Context : DbContext
{
    public DbSet<Models.Session> Sessions { get; set; }
    public DbSet<Models.Application> Applications { get; set; }
    
    public Context(DbContextOptions<Context> options) : base(options)
    {
    }
}