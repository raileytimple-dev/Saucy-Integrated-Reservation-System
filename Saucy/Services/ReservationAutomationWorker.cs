namespace SaucyMVC.Services;

public sealed class ReservationAutomationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationAutomationWorker> _logger;

    public ReservationAutomationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationAutomationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessOnceAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcessOnceAsync(stoppingToken);
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var automation = scope.ServiceProvider.GetRequiredService<ReservationAutomationService>();
            var result = await automation.ProcessAsync(cancellationToken);
            if (result.TotalChanges > 0)
                _logger.LogInformation(
                    "Reservation automation expired {ExpiredHolds} holds, released {NoShows} no-shows, and moved {Tables} tables to cleaning.",
                    result.ExpiredHolds, result.ReleasedNoShows, result.TablesMovedToCleaning);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Reservation automation pass failed; it will retry in one minute.");
        }
    }
}
