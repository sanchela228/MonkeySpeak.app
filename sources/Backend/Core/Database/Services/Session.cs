using Context = Core.Database.Context;
using SessionModel = Core.Database.Models.Session;
using SessionStatus = Core.Database.Models.SessionStatus;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class Session(Context db)
{
    public async Task<SessionModel> CreateSession( string applicationId, Guid sessionId, string? meta = null )
    {
        var session = new SessionModel()
        {
            ApplicationId = applicationId,
            WebSocketSessionId = sessionId
        };
        
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
    
    public async Task<SessionModel?> GetSessionByCode(string code)
    {
        return await db.Sessions
            .FirstOrDefaultAsync(s => s.Code == code && s.Status == SessionStatus.Active);
    }
    
    public async Task CloseSession(Guid webSocketSessionId)
    {
        var session = await db.Sessions
            .FirstOrDefaultAsync(s => s.WebSocketSessionId == webSocketSessionId);
            
        if (session != null)
        {
            session.Status = SessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
    
    public async Task CleanupExpiredSessions(TimeSpan expirationTime)
    {
        var expiredSessions = await db.Sessions
            .Where(s => s.Status == SessionStatus.Active && 
                        s.CreatedAt < DateTime.UtcNow.Subtract(expirationTime))
            .ToListAsync();
            
        foreach (var session in expiredSessions)
        {
            session.Status = SessionStatus.Expired;
            session.ClosedAt = DateTime.UtcNow;
        }
        
        await db.SaveChangesAsync();
    }
}