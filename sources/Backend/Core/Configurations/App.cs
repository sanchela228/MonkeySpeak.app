using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Core.Services;
using Core.Websockets;
using Microsoft.EntityFrameworkCore;
using ContextDatabase = Core.Database.Context;

namespace MonkeySpeak.Backend.Core.Configurations;

public class App
{
    static App() => Instance = new();
    public static App Instance { get; private set; }
    
    public Context DbContext { get; }
    
    public static List<Connection> connections = new();
    public static List<Room> rooms = new();
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
    
    public async static void InitWebsockets(WebApplication app)
    {
        app.UseWebSockets(webSocketOptions);
        
        app.Map("/connector", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                using var scope = app.Services.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<ContextDatabase>();
                
                var websocketMiddleware = new WebsocketMiddleware(webSocket, dbContext, connections, rooms);
                await websocketMiddleware.OpenWebsocketConnection(context);
            }
            else context.Response.StatusCode = StatusCodes.Status400BadRequest;
        });
        
        // DEMO TEST WHILE UDP NOT WORK
        app.MapGet("/get-public-endpoint", async (HttpContext context) =>
        {
           
            var remoteIp = context.Connection.RemoteIpAddress;
            var remotePort = context.Connection.RemotePort;

            string response = $"{remoteIp}:{remotePort}";
            await context.Response.WriteAsync(response);
        });
        
        app.UseWebSockets();

        Task.Run(RunUdpStunTest);
    }

    public static void RunUdpStunTest()
    {
        Console.WriteLine("Starting UDP STUN server on port 3478...");
        using (UdpClient udp = new UdpClient(3478))
        {
            IPEndPoint remoteEP = null;
            Console.WriteLine("UDP STUN server started successfully on port 3478");

            while (true)
            {
                try
                {
                    byte[] data = udp.Receive(ref remoteEP);
                    Console.WriteLine($"Got request from {remoteEP}");

                    string response = $"{remoteEP.Address}:{remoteEP.Port}";
                    byte[] respBytes = Encoding.UTF8.GetBytes(response);

                    int bytesSent = udp.Send(respBytes, respBytes.Length, remoteEP);
                    Console.WriteLine($"Sent {bytesSent} bytes to {remoteEP}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server error: {ex.Message}");
                    remoteEP = null;
                }
            }
        }
    }
    
    public static async Task ApplyMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContextDatabase>();
    
        await dbContext.Database.MigrateAsync();
    }
}