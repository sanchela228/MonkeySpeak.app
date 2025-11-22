using Core.Database;
using Core.Http.Resources;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using MonkeySpeak.Backend.Core.Http;

namespace Core.Http.Controllers;

public class Sessions(Context db) : ControllerBase
{
    private readonly Session SessionService = new(db);
    
    [HttpPost]
    [Route("/api/sessions/verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyRequest request)
    {
        try
        {
            if ( db.Applications.Any(x => x.ApplicationId == request.ApplicationId) == false )
                return BadRequest(new Response(false, "Application unrecognized."));
        
            var session = await SessionService.GetSessionByCode(request.Code);
        
            if (session is null)
                return BadRequest(new Response(false, "Session not found."));
        
            return Ok( new Response(true, "Session found.", new
            {
                sessionId = session.WebSocketSessionId
            }) );
        }
        catch (Exception e)
        {
            return StatusCode(500, new Response(false, $"Internal server error: {e.Message}"));
        }
    }
}