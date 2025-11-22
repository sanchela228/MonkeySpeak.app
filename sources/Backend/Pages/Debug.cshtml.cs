using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using Core.Websockets;
using Microsoft.Extensions.Options;
using MonkeySpeak.Backend.Core.Configurations;

namespace Pages
{
    public class DebugModel(IOptions<App> appInfo) : PageModel
    {
        public List<Connection> GetAllConnections() => App.connections;
        public List<Room> GetAllRooms() => App.rooms;
    }
}