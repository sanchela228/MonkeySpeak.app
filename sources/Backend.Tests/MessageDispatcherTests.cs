using System;
using System.Collections.Concurrent;
using System.Linq;
using Backend.Tests.Mocks;
using Core.Websockets;
using Core.Websockets.Messages.NoAuthCall;
using Xunit;

namespace Backend.Tests.Unit;

public class MessageDispatcherTests
{
    private MessageDispatcher CreateDispatcher(ConcurrentDictionary<Guid, Connection> connections, ConcurrentDictionary<string, Room> rooms)
    {
        var dispatcher = new MessageDispatcher();
        
        dispatcher.Configure(null!, connections, rooms, null!);
        return dispatcher;
    }

    [Fact]
    public void CreateSession_ShouldCreateRoomAndReturnCode()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        var creator = new MockConnection();
        creator.PublicIp = "192.168.1.1:5000";
        connections.TryAdd(creator.Id, creator);

        var createMsg = new CreateSession { IpEndPoint = "192.168.1.1:5000" };
        var context = Context.Create(createMsg);

        // Act
        dispatcher.Dispatch(context, creator);

        // Assert
        Assert.Single(rooms);
        var createdRoom = rooms.Values.Single();
        Assert.Equal(Room.RoomState.Waiting, createdRoom.State);
        Assert.True(createdRoom.Connections.ContainsKey(creator.Id));
        
        var response = creator.GetLastMessage<SessionCreated>();
        Assert.NotNull(response);
        Assert.NotEmpty(response.Value);
        Assert.Equal(creator.Id.ToString(), response.SelfInterlocutorId);
    }

    [Fact]
    public void ConnectToSession_WithInvalidCode_ShouldReturnError()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        var client = new MockConnection();
        connections.TryAdd(client.Id, client);

        var connectMsg = new ConnectToSession 
        { 
            Code = "invalid", 
            IpEndPoint = "192.168.1.2:5000" 
        };
        var context = Context.Create(connectMsg);

        // Act
        dispatcher.Dispatch(context, client);

        // Assert
        var error = client.GetLastMessage<ErrorConnectToSession>();
        Assert.NotNull(error);
    }

    [Fact]
    public void ConnectToSession_ShouldAddToRoomAndBroadcastHolePunching()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        var creator = new MockConnection();
        creator.PublicIp = "192.168.1.1:5000";
        var room = new Room(creator);
        rooms.TryAdd(room.Code, room);
        connections.TryAdd(creator.Id, creator);

        var joiner = new MockConnection();
        joiner.PublicIp = "192.168.1.2:5000";
        connections.TryAdd(joiner.Id, joiner);

        var connectMsg = new ConnectToSession 
        { 
            Code = room.Code, 
            IpEndPoint = "192.168.1.2:5000" 
        };
        var context = Context.Create(connectMsg);

        // Act
        dispatcher.Dispatch(context, joiner);

        // Assert
        Assert.Equal(2, room.Connections.Count);
        Assert.Contains(joiner.Id, room.Connections);
        Assert.Equal(Room.RoomState.Running, room.State);

        var creatorMsg = creator.GetLastMessage<HolePunching>();
        Assert.NotNull(creatorMsg);
        Assert.Equal("192.168.1.2:5000", creatorMsg.IpEndPoint);
        Assert.Equal(joiner.Id.ToString(), creatorMsg.InterlocutorId);

        var joinerMsg = joiner.GetLastMessage<HolePunching>();
        Assert.NotNull(joinerMsg);
        Assert.Equal("192.168.1.1:5000", joinerMsg.IpEndPoint);
        Assert.Equal(creator.Id.ToString(), joinerMsg.InterlocutorId);
    }

    [Fact]
    public void ConnectToSession_ShouldOnlyBroadcastToSameRoom()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        // 1
        var creator1 = new MockConnection();
        creator1.PublicIp = "192.168.1.1:5000";
        var room1 = new Room(creator1);
        rooms.TryAdd(room1.Code, room1);
        connections.TryAdd(creator1.Id, creator1);

        // 2
        var creator2 = new MockConnection();
        creator2.PublicIp = "192.168.1.3:5000";
        var room2 = new Room(creator2);
        rooms.TryAdd(room2.Code, room2);
        connections.TryAdd(creator2.Id, creator2);

        var joiner = new MockConnection();
        joiner.PublicIp = "192.168.1.2:5000";
        connections.TryAdd(joiner.Id, joiner);

        var connectMsg = new ConnectToSession 
        { 
            Code = room1.Code, 
            IpEndPoint = "192.168.1.2:5000" 
        };
        var context = Context.Create(connectMsg);

        // Act
        dispatcher.Dispatch(context, joiner);

        // Assert
        Assert.NotEmpty(creator1.SentMessages);
        Assert.Empty(creator2.SentMessages);
    }

    [Fact]
    public void HangupSession_ShouldRemoveParticipantAndNotifyOthers()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        var creator = new MockConnection();
        var joiner1 = new MockConnection();
        var joiner2 = new MockConnection();
        
        var room = new Room(creator);
        room.Connections.TryAdd(joiner1.Id, joiner1);
        room.Connections.TryAdd(joiner2.Id, joiner2);
        room.SetState(Room.RoomState.Running);
        rooms.TryAdd(room.Code, room);

        creator.ClearMessages();
        joiner1.ClearMessages();
        joiner2.ClearMessages();

        var hangupMsg = new HangupSession();
        var context = Context.Create(hangupMsg);

        // Act
        dispatcher.Dispatch(context, joiner1);

        // Assert
        Assert.Equal(2, room.Connections.Count);
        Assert.DoesNotContain(joiner1.Id, room.Connections);
        
        Assert.NotEmpty(creator.SentMessages);
        Assert.NotEmpty(joiner2.SentMessages);
        
        Assert.Contains(room.Code, rooms);
    }

    [Fact]
    public void HangupSession_LastParticipant_ShouldRemoveRoom()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        var creator = new MockConnection();
        var room = new Room(creator);
        rooms.TryAdd(room.Code, room);

        var hangupMsg = new HangupSession();
        var context = Context.Create(hangupMsg);

        // Act
        dispatcher.Dispatch(context, creator);

        // Assert
        Assert.Empty(rooms);
    }

    [Fact]
    public void HangupSession_TwoRemaining_ShouldSetStateToWaiting()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        var creator = new MockConnection();
        var joiner1 = new MockConnection();
        var joiner2 = new MockConnection();
        
        var room = new Room(creator);
        room.Connections.TryAdd(joiner1.Id, joiner1);
        room.Connections.TryAdd(joiner2.Id, joiner2);
        room.SetState(Room.RoomState.Running);
        rooms.TryAdd(room.Code, room);

        var hangupMsg = new HangupSession();
        var context = Context.Create(hangupMsg);

        // Act
        dispatcher.Dispatch(context, joiner2);

        // Assert
        Assert.Equal(2, room.Connections.Count);
        Assert.Equal(Room.RoomState.Running, room.State);
    }

    [Fact]
    public void ConnectToSession_WithEmptyCode_ShouldReturnError()
    {
        // Arrange
        var connections = new ConcurrentDictionary<Guid, Connection>();
        var rooms = new ConcurrentDictionary<string, Room>();
        var dispatcher = CreateDispatcher(connections, rooms);
        
        var client = new MockConnection();
        connections.TryAdd(client.Id, client);

        var connectMsg = new ConnectToSession 
        { 
            Code = "", 
            IpEndPoint = "192.168.1.2:5000" 
        };
        var context = Context.Create(connectMsg);

        // Act
        dispatcher.Dispatch(context, client);

        // Assert
        var error = client.GetLastMessage<ErrorConnectToSession>();
        Assert.NotNull(error);
        Assert.Equal("Invalid parameters", error.Value);
    }
}