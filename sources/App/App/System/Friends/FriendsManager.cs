using App.System.Models.Websocket.Messages.AuthCall;
using App.System.Modules;
using App.System.Services;

namespace App.System.Friends;

public class FriendsManager
{
    private readonly WebSocketClient _webSocketClient;
    private readonly HashSet<string> _pendingRequestActionsInFlight = new();
    
    public List<FriendInfo> Friends { get; private set; } = new();
    public List<FriendRequestReceived> PendingRequests { get; private set; } = new();
    
    public event Action? OnFriendsUpdated;
    public event Action? OnPendingRequestsUpdated;
    public event Action<string>? OnFriendRequestSent;
    public event Action<FriendRequestReceived>? OnFriendRequestReceived;
    public event Action<string>? OnFriendAccepted;

    public FriendsManager(WebSocketClient webSocketClient)
    {
        _webSocketClient = webSocketClient;
        
        _webSocketClient.MessageDispatcher.On<FriendListResponse>(HandleFriendListResponse);
        _webSocketClient.MessageDispatcher.On<PendingFriendListResponse>(HandlePendingFriendListResponse);
        _webSocketClient.MessageDispatcher.On<FriendRequestSent>(HandleFriendRequestSent);
        _webSocketClient.MessageDispatcher.On<FriendRequestReceived>(HandleFriendRequestReceived);
        _webSocketClient.MessageDispatcher.On<FriendAccepted>(HandleFriendAccepted);
        _webSocketClient.MessageDispatcher.On<FriendRequestRejected>(HandleFriendRequestRejected);
    }
    
    public async Task GetFriendListAsync()
    {
        try
        {
            await _webSocketClient.SendAsync(new GetFriendList
            {
                Value = "Get friend list"
            });
            
            Logger.Write(Logger.Type.Info, "Requested friend list");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to request friend list", ex);
        }
    }
    
    public async Task GetPendingFriendListAsync()
    {
        try
        {
            await _webSocketClient.SendAsync(new GetPendingFriendList
            {
                Value = "Get pending friend list"
            });
            
            Logger.Write(Logger.Type.Info, "Requested pending friend list");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to request pending friend list", ex);
        }
    }
    
    public async Task AddFriendAsync(string friendUsername)
    {
        try
        {
            await _webSocketClient.SendAsync(new AddFriend
            {
                FriendUsername = friendUsername,
                Value = "Add friend"
            });
            
            Logger.Write(Logger.Type.Info, $"Sent friend request to: {friendUsername}");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to add friend", ex);
        }
    }
    
    public async Task AcceptFriendAsync(string friendshipId)
    {
        if (string.IsNullOrWhiteSpace(friendshipId))
            return;

        if (!_pendingRequestActionsInFlight.Add(friendshipId))
            return;

        var removed = PendingRequests.RemoveAll(r => r.FriendshipId == friendshipId) > 0;
        if (removed)
        {
            OnFriendsUpdated?.Invoke();
            OnPendingRequestsUpdated?.Invoke();
        }

        try
        {
            await _webSocketClient.SendAsync(new AcceptFriend
            {
                FriendshipId = friendshipId,
                Value = "Accept friend"
            });
            
            Logger.Write(Logger.Type.Info, $"Accepted friend request: {friendshipId}");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to accept friend", ex);
            _ = GetPendingFriendListAsync();
        }
        finally
        {
            _pendingRequestActionsInFlight.Remove(friendshipId);
        }
    }
    
    public async Task RejectFriendAsync(string friendshipId)
    {
        if (string.IsNullOrWhiteSpace(friendshipId))
            return;

        if (!_pendingRequestActionsInFlight.Add(friendshipId))
            return;

        var removed = PendingRequests.RemoveAll(r => r.FriendshipId == friendshipId) > 0;
        if (removed)
        {
            OnFriendsUpdated?.Invoke();
            OnPendingRequestsUpdated?.Invoke();
        }

        try
        {
            await _webSocketClient.SendAsync(new RejectFriend
            {
                FriendshipId = friendshipId,
                Value = "Reject friend"
            });
            
            Logger.Write(Logger.Type.Info, $"Rejected friend request: {friendshipId}");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to reject friend", ex);
            _ = GetPendingFriendListAsync();
        }
        finally
        {
            _pendingRequestActionsInFlight.Remove(friendshipId);
        }
    }

    private void HandleFriendListResponse(FriendListResponse message)
    {
        Friends = message.Friends;
        OnFriendsUpdated?.Invoke();
        Logger.Write(Logger.Type.Info, $"Received friend list: {Friends.Count} friends");
    }
    
    private void HandlePendingFriendListResponse(PendingFriendListResponse message)
    {
        PendingRequests = message.Friends;
        OnFriendsUpdated?.Invoke();
        OnPendingRequestsUpdated?.Invoke();
        Logger.Write(Logger.Type.Info, $"Received pending friend list: {PendingRequests.Count} requests");
    }

    private void HandleFriendRequestSent(FriendRequestSent message)
    {
        Logger.Write(Logger.Type.Info, $"Friend request sent to: {message.FriendUsername}");
        OnFriendRequestSent?.Invoke(message.FriendUsername);
    }

    private void HandleFriendRequestReceived(FriendRequestReceived message)
    {
        Logger.Write(Logger.Type.Info, $"Friend request received from: {message.FromUsername}");
        PendingRequests.Add(message);
        OnFriendRequestReceived?.Invoke(message);
    }

    private void HandleFriendAccepted(FriendAccepted message)
    {
        Logger.Write(Logger.Type.Info, $"Friend accepted: {message.FriendUsername}");
        OnFriendAccepted?.Invoke(message.FriendUsername);
        
        PendingRequests.RemoveAll(r => r.FriendshipId == message.FriendshipId);
        OnPendingRequestsUpdated?.Invoke();
        
        _ = GetFriendListAsync();
    }

    private void HandleFriendRequestRejected(FriendRequestRejected message)
    {
        Logger.Write(Logger.Type.Info, $"Friend request rejected: {message.FriendshipId}");
        
        PendingRequests.RemoveAll(r => r.FriendshipId == message.FriendshipId);
        OnPendingRequestsUpdated?.Invoke();
    }
}
