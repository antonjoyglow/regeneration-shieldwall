using System.Diagnostics;
using ShieldWall.Shared.Interfaces;
using ShieldWall.Shared.Models;

namespace ShieldWall.TeamKit.Services;

public sealed class AlertPipeline(
    SentinelConnection connection,
    IAlertClassifier classifier,
    IPatternDetector patternDetector,
    IResponseEngine responseEngine,
    ILogger<AlertPipeline> logger) : IDisposable
{
    private readonly List<ClassifiedAlert> _classifiedHistory = [];
    private readonly Lock _lock = new();
    private List<ThreatPattern> _lastPatterns = [];
    private bool _disposed;

    public void Initialize()
    {
        connection.AlertReceived += OnAlertReceivedAsync;
        logger.LogInformation("Alert processing pipeline initialized");
    }

    private async Task OnAlertReceivedAsync(SentinelAlert alert)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var classified = classifier.Classify(alert);

            List<ClassifiedAlert> history;
            lock (_lock)
            {
                _classifiedHistory.Add(classified);
                if (_classifiedHistory.Count > 50)
                    _classifiedHistory.RemoveAt(0);
                history = [.. _classifiedHistory];
            }

            var patterns = patternDetector.Detect(history);
            var response = responseEngine.Decide(classified, patterns);

            stopwatch.Stop();

            var decision = new TeamDecision
            {
                AlertId = alert.AlertId,
                Classification = classified,
                Patterns = patterns.AsReadOnly(),
                Response = response,
                ProcessedAt = DateTime.UtcNow,
                ProcessingDurationMs = stopwatch.Elapsed.TotalMilliseconds
            };

            await connection.SubmitDecisionAsync(decision, CancellationToken.None);

            logger.LogInformation(
                "Processed {AlertId}: {ThreatLevel} -> {Action} (score: {Score:F2})",
                alert.AlertId, classified.ThreatLevel, response.Action, classified.ComputedScore);

            if (patterns.Count > 0 && !PatternsEqual(patterns, _lastPatterns))
            {
                var report = new TeamPatternReport(
                    connection.TeamName,
                    patterns.AsReadOnly(),
                    DateTime.UtcNow);

                await connection.SubmitPatternsAsync(report, CancellationToken.None);

                lock (_lock)
                {
                    _lastPatterns = patterns;
                }

                logger.LogInformation(
                    "Submitted {PatternCount} threat pattern(s) for {AlertId}",
                    patterns.Count, alert.AlertId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline error processing alert {AlertId}", alert.AlertId);
        }
    }

    private static bool PatternsEqual(List<ThreatPattern> a, List<ThreatPattern> b)
    {
        if (a.Count != b.Count) return false;
        return a.Select(p => p.PatternId).SequenceEqual(b.Select(p => p.PatternId));
    }

    public void Dispose()
    {
        if (_disposed) return;
        connection.AlertReceived -= OnAlertReceivedAsync;
        _disposed = true;
    }
}
