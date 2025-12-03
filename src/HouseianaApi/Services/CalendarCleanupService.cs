namespace HouseianaApi.Services;

/// <summary>
/// Background service that periodically cleans up expired calendar holds
/// </summary>
public class CalendarCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CalendarCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public CalendarCleanupService(IServiceProvider serviceProvider, ILogger<CalendarCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Calendar Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();

                var releasedCount = await availabilityService.ReleaseExpiredHoldsAsync();

                if (releasedCount > 0)
                {
                    _logger.LogInformation("Released {Count} expired calendar holds", releasedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Calendar Cleanup Service");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
