using System.Net.WebSockets;
using Core.Database;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonkeySpeak.Backend.Core.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<App>(builder.Configuration.GetSection("App"));

builder.Services.AddRazorPages();

App.BuildDatabase(builder);

builder.Services.AddControllers();

var app = builder.Build();

App.InitWebsockets(app);

app.MapRazorPages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

await App.ApplyMigrations(app);

app.Run();