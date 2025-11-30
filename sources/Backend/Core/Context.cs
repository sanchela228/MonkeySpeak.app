using System;
using System.Collections.Concurrent;
using Core.Websockets;

namespace Backend.Core;

public static class Context
{
    public static ConcurrentDictionary<Guid, Connection> Connections = new();
    public static ConcurrentDictionary<string, Room> Rooms = new();
}