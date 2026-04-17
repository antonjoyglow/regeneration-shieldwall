using Microsoft.AspNetCore.SignalR;
using ShieldWall.GameMaster.Hubs;
using ShieldWall.GameMaster.Services;
using ShieldWall.Shared.Hubs;

var builder = WebApplication.CreateBuilder(args);

// SignalR
builder.Services.AddSignalR();

// Services
builder.Services.AddSingleton<ITeamTracker, TeamTracker>();

// F4 — Scenario Engine & Phase Management
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ScenarioLoader>();
builder.Services.AddSingleton<PhaseManager>();
builder.Services.AddSingleton<IPhaseManager>(sp => sp.GetRequiredService<PhaseManager>());

// F5 — Scoring Engine
builder.Services.AddSingleton<IScoringEngine, ScoringEngine>();
builder.Services.AddSingleton<IAlertStreamEngine, AlertStreamEngine>();

// F12 — Game Orchestrator & Auth
builder.Services.AddSingleton<IGameOrchestrator, GameOrchestrator>();
builder.Services.AddSingleton<GameMasterAuthService>();

// CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();

// ── Helper: extract and validate GM token ─────────────────────────────────

static bool IsGmAuthorized(HttpContext http) =>
    http.RequestServices.GetRequiredService<GameMasterAuthService>()
        .ValidateToken(http.Request.Headers.Authorization.ToString().Replace("Bearer ", ""));

// ── Public endpoints ──────────────────────────────────────────────────────

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/api/status", (
    ITeamTracker teamTracker,
    IAlertStreamEngine engine,
    IPhaseManager phaseManager,
    IGameOrchestrator orchestrator) =>
{
    var teams = teamTracker.GetAllTeams();
    return Results.Ok(new
    {
        gamePhase = orchestrator.CurrentGamePhase.ToString(),
        streamRunning = engine.IsRunning,
        streamPaused = engine.IsPaused,
        currentPhase = phaseManager.CurrentPhase,
        connectedTeams = teams.Select(static t => new
        {
            t.TeamName,
            t.IsConnected,
            t.MissionEffectiveness,
            t.AlertsProcessed,
            t.AverageLatencyMs
        }),
        teamCount = teams.Count,
        connectedCount = teams.Count(static t => t.IsConnected)
    });
});

// ── GM Auth ───────────────────────────────────────────────────────────────

app.MapPost("/api/facilitator/login", (HttpContext http, GameMasterAuthService auth) =>
{
    var form = http.Request.ReadFromJsonAsync<LoginRequest>(http.RequestAborted).Result;
    if (form is null)
        return Results.BadRequest("Invalid request body.");

    var token = auth.Login(form.Username, form.Password);
    return token is not null
        ? Results.Ok(new { token })
        : Results.Unauthorized();
});

// ── GM Config ─────────────────────────────────────────────────────────────

app.MapGet("/api/facilitator/config", (HttpContext http, IConfiguration config) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    var quickAnnouncements = config.GetSection("GameMaster:QuickAnnouncements").Get<string[]>() ?? [];
    return Results.Ok(new { quickAnnouncements });
});

// ── GM Lifecycle ──────────────────────────────────────────────────────────

app.MapPost("/api/facilitator/briefing", async (HttpContext http, IGameOrchestrator orchestrator, CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    await orchestrator.StartBriefingAsync(ct);
    return Results.Ok(new { phase = orchestrator.CurrentGamePhase.ToString() });
});

app.MapPost("/api/facilitator/start", async (HttpContext http, IGameOrchestrator orchestrator, CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    await orchestrator.StartStreamAsync(ct);
    return Results.Ok(new { phase = orchestrator.CurrentGamePhase.ToString() });
});

app.MapPost("/api/facilitator/pause", async (HttpContext http, IGameOrchestrator orchestrator, CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    await orchestrator.TogglePauseAsync(ct);
    return Results.Ok(new { phase = orchestrator.CurrentGamePhase.ToString() });
});

app.MapPost("/api/facilitator/end", async (HttpContext http, IGameOrchestrator orchestrator, CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    await orchestrator.EndGameAsync(ct);
    return Results.Ok(new { phase = orchestrator.CurrentGamePhase.ToString() });
});

app.MapPost("/api/facilitator/reset", async (HttpContext http, IGameOrchestrator orchestrator, CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    await orchestrator.ResetAsync(ct);
    return Results.Ok(new { phase = orchestrator.CurrentGamePhase.ToString() });
});

// ── GM Wave Dispatch ──────────────────────────────────────────────────────

app.MapPost("/api/facilitator/wave/{count:int}", async (
    int count,
    HttpContext http,
    IGameOrchestrator orchestrator,
    CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();
    if (count is < 1 or > 50) return Results.BadRequest("Count must be between 1 and 50.");

    var dispatched = await orchestrator.DispatchWaveAsync(count, ct);
    return Results.Ok(new
    {
        dispatched = dispatched.Count,
        alerts = dispatched.Select(static a => new
        {
            a.AlertId,
            a.Type,
            a.Sector,
            a.RawSeverity,
            a.ConfidenceScore,
            a.Source,
            Answer = $"{a.GroundTruth.CorrectClassification} / {a.GroundTruth.CorrectAction}"
        })
    });
});

// ── GM Phase Dispatch / Replay ────────────────────────────────────────────

app.MapPost("/api/facilitator/phase/{phaseNumber:int}/dispatch", async (
    int phaseNumber,
    HttpContext http,
    IGameOrchestrator orchestrator,
    CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();
    if (phaseNumber is < 1 or > 4) return Results.BadRequest("Phase number must be between 1 and 4.");

    var dispatched = await orchestrator.DispatchPhaseAsync(phaseNumber, ct);
    return Results.Ok(new
    {
        phase = phaseNumber,
        dispatched = dispatched.Count,
        alerts = dispatched.Select(static a => new
        {
            a.AlertId,
            a.Type,
            a.Sector,
            a.RawSeverity,
            a.ConfidenceScore,
            a.Source,
            Answer = $"{a.GroundTruth.CorrectClassification} / {a.GroundTruth.CorrectAction}"
        })
    });
});

app.MapPost("/api/facilitator/phase/{phaseNumber:int}/replay", async (
    int phaseNumber,
    HttpContext http,
    IGameOrchestrator orchestrator,
    CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();
    if (phaseNumber is < 1 or > 4) return Results.BadRequest("Phase number must be between 1 and 4.");

    var dispatched = await orchestrator.ReplayPhaseAsync(phaseNumber, ct);
    return Results.Ok(new
    {
        phase = phaseNumber,
        dispatched = dispatched.Count,
        alerts = dispatched.Select(static a => new
        {
            a.AlertId,
            a.Type,
            a.Sector,
            a.RawSeverity,
            a.ConfidenceScore,
            a.Source,
            Answer = $"{a.GroundTruth.CorrectClassification} / {a.GroundTruth.CorrectAction}"
        })
    });
});

// ── GM Alert View ─────────────────────────────────────────────────────────

app.MapGet("/api/facilitator/alerts/upcoming", (HttpContext http, IAlertStreamEngine engine) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    var count = int.TryParse(http.Request.Query["count"], out var c) ? c : 10;
    var upcoming = engine.GetUpcomingAlerts(Math.Clamp(count, 1, 50));
    return Results.Ok(upcoming.Select(static a => new
    {
        a.AlertId,
        a.Type,
        a.Sector,
        a.RawSeverity,
        a.ConfidenceScore,
        a.Source,
        a.BroadcastOffsetSeconds,
        a.CorrelationGroup,
        GroundTruth = new
        {
            Classification = a.GroundTruth.CorrectClassification.ToString(),
            Action = a.GroundTruth.CorrectAction.ToString(),
            a.GroundTruth.IsCompoundMember,
            a.GroundTruth.CompoundGroupId
        }
    }));
});

app.MapGet("/api/facilitator/alerts/progress", (HttpContext http, IAlertStreamEngine engine) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();
    return Results.Ok(engine.GetProgress());
});

// ── GM Announcements ──────────────────────────────────────────────────────

app.MapPost("/api/facilitator/announce", async (
    HttpContext http,
    IHubContext<SentinelHub, ISentinelHubClient> hub,
    CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    using var reader = new StreamReader(http.Request.Body);
    var body = await reader.ReadToEndAsync(ct);
    if (string.IsNullOrWhiteSpace(body))
        return Results.BadRequest("Message body required.");
    await hub.Clients.Group("broadcast").ReceiveAnnouncement(body);
    return Results.Ok(new { announced = body });
});

// ── GM Phase Force ────────────────────────────────────────────────────────

app.MapPost("/api/facilitator/phase/{n:int}", async (
    int n,
    HttpContext http,
    IPhaseManager phaseManager,
    CancellationToken ct) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();

    await phaseManager.ForcePhaseAsync(n, ct);
    return Results.Ok(new { phase = phaseManager.CurrentPhase });
});

// ── Results Export ─────────────────────────────────────────────────────────

app.MapGet("/api/results/export", (HttpContext http, IScoringEngine scoring) =>
{
    if (!IsGmAuthorized(http)) return Results.Unauthorized();
    return Results.Ok(scoring.ExportResults());
});

// Hub
app.MapHub<SentinelHub>("/sentinel-hub");

app.Run();

// ── Request models ────────────────────────────────────────────────────────

record LoginRequest(string Username, string Password);