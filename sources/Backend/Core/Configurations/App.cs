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
            options.UseNpgsql(connectionString));
        
        builder.Services.AddScoped<Session>();
    }
    
    static WebSocketOptions webSocketOptions = new() {
        KeepAliveInterval = TimeSpan.FromMinutes(2)
    };

    public static async Task ApplyMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContextDatabase>();
    
        await dbContext.Database.MigrateAsync();
    }
}