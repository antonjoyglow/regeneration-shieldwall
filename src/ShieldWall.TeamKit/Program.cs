using ShieldWall.Shared.Interfaces;
using ShieldWall.TeamKit.Services;
using ShieldWall.TeamKit.State;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SentinelConnection>();
builder.Services.AddSingleton<GameState>();
builder.Services.AddHostedService<SentinelConnectionHost>();

// Alert processing pipeline & participant service implementations
builder.Services.AddSingleton<IAlertClassifier, AlertClassifier>();
builder.Services.AddSingleton<IPatternDetector, PatternDetector>();
builder.Services.AddSingleton<IResponseEngine, ResponseEngine>();
builder.Services.AddSingleton<AlertPipeline>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<ShieldWall.TeamKit.Components.App>()
    .AddInteractiveServerRenderMode();

// Initialize the alert processing pipeline (subscribes to incoming alerts)
app.Services.GetRequiredService<AlertPipeline>().Initialize();
app.Services.GetRequiredService<GameState>().EnsureSubscribed();

app.Run();