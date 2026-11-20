namespace RealEstateAI.Api.Services;

/// <summary>
/// Periodically re-runs saved searches and creates in-app notifications for new matches.
/// </summary>
public class AlertWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AlertWorker> _logger;

    public AlertWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<AlertWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = int.TryParse(_config["Alerts:IntervalMinutes"], out var parsed) ? parsed : 2;
        var interval = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 1440));

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var savedSearches = scope.ServiceProvider.GetRequiredService<ISavedSearchService>();

                var ids = await savedSearches.GetAllEnabledIdsAsync();
                foreach (var id in ids)
                    await savedSearches.RunAsync(id);

                if (ids.Count > 0)
                    _logger.LogInformation("Alert worker checked {Count} saved searches.", ids.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alert worker run failed.");
            }
        }
    }
}
