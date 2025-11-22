using Core.Database;
using Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Core.Http.Controllers;

public class Application(Context db) : ControllerBase
{
    [HttpPost]
    [Route("/api/applications/register")]
    public async Task CreateApplication([FromBody] string id)
    {
        if (db.Applications.Any(x => x.ApplicationId == id)) 
            return;
        
        await db.Applications.AddAsync(new Core.Database.Models.Application()
        {
            ApplicationId = id
        });

        await db.SaveChangesAsync();
    }
}