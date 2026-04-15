using Microsoft.AspNetCore.SignalR.Client;
using ShieldWall.Shared.Models;

namespace ShieldWall.TeamKit.Services;

public sealed class SentinelConnectionHost(
    SentinelConnection connection,
    ILogger<SentinelConnectionHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                {
                    await connection.ConnectAsync(stoppingToken);
                    logger.LogInformation("Connected to Game Master");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                await connection.SendHeartbeatAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Connection attempt failed — retrying in 5 seconds");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
