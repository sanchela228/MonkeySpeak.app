using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using Core.Websockets;
using Microsoft.Extensions.Options;
using Core.Configurations;

namespace Pages
{
    public class DebugModel(IOptions<App> appInfo) : PageModel
    {
        public ConcurrentDictionary<Guid, Connection> GetAllConnections() => App.connections;
        public ConcurrentDictionary<string, Room> GetAllRooms() => App.rooms;
    }
}