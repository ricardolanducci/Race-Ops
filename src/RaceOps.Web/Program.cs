using Microsoft.EntityFrameworkCore;
using RaceOps.Application.UseCases;
using RaceOps.Domain.Interfaces;
using RaceOps.Infrastructure.Database;
using RaceOps.Infrastructure.Live;
using RaceOps.Infrastructure.RaceMonitor;
using RaceOps.Web.Hubs;
using RaceOps.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configuração ---
var apiToken = builder.Configuration["RaceMonitor:ApiToken"]
    ?? throw new InvalidOperationException("RaceMonitor:ApiToken não configurado");
var baseUrl = builder.Configuration["RaceMonitor:BaseUrl"]
    ?? "https://api.race-monitor.com/";

// --- Serviços ---
builder.Services.AddHttpClient<IRaceMonitorClient, RaceMonitorClient>(client =>
{
    client.BaseAddress = new Uri(baseUrl);
}).AddTypedClient<IRaceMonitorClient>((http, _) => new RaceMonitorClient(http, apiToken));

builder.Services.AddScoped<GetCurrentRacesUseCase>();
builder.Services.AddScoped<GetLiveSessionUseCase>();
builder.Services.AddScoped<GetRacerDetailUseCase>();
builder.Services.AddScoped<GetStreamingConnectionUseCase>();
builder.Services.AddScoped<GetLiveViewUseCase>();
builder.Services.AddScoped<GetEventConfigUseCase>();
builder.Services.AddScoped<SaveEventConfigUseCase>();
builder.Services.AddScoped<SaveStintUseCase>();

builder.Services.AddScoped<IEventConfigRepository, EventConfigRepository>();
builder.Services.AddScoped<IStintRepository, StintRepository>();

builder.Services.AddSingleton<LiveStreamService>();
builder.Services.AddSingleton<ILiveRaceMonitor, LiveRaceMonitor>();
builder.Services.AddHostedService<LiveBroadcastService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=raceops.db"));

builder.Services.AddSignalR();
builder.Services.AddControllers();

var app = builder.Build();

// --- Migrações ---
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

// --- Pipeline ---
app.UseStaticFiles();
app.MapControllers();
app.MapHub<RaceHub>("/hubs/race");

// SPA fallback — serve index.html para rotas não-api
app.MapFallbackToFile("index.html");

app.Run();
