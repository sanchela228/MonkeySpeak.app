using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Core.Services;
using Core.Websockets;
using Microsoft.EntityFrameworkCore;
using ContextDatabase = Core.Database.Context;

namespace Core.Configurations;

public class App
{
    static App() => Instance = new();
    public static App Instance { get; private set; }
    
    public Context DbContext { get; }
    public string BackendVersion { get; set; }
   
    public string FrontendVersion { get; set; }
    public string FrontendAdditionalVersionText { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    public static void BuildDatabase(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        
        builder.Services.AddDbContext<ContextDatabase>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("monkeyspeakbackend")));
        
        builder.Services.AddScoped<Session>();
        builder.Services.AddScoped<Database.Services.UserService>();
        builder.Services.AddScoped<Database.Services.FriendshipService>();
    }
    
    static WebSocketOptions webSocketOptions = new() {
        KeepAliveInterval = TimeSpan.FromMinutes(2)
    };

    public static async Task ApplyMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContextDatabase>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<App>>();

        const int maxRetries = 10;
        const int delayMs = 3000;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                logger.LogInformation("Applying migrations (attempt {Attempt}/{Max})...", i + 1, maxRetries);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Migration attempt {Attempt} failed. Retrying in {Delay}ms...", i + 1, delayMs);
                await Task.Delay(delayMs);
            }
        }

        throw new Exception("Failed to apply migrations after maximum retries.");
    }
}