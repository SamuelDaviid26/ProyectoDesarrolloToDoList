using Microsoft.Extensions.Options;
using ToDoApi.Services;

namespace ToDoApi.BackgroundServices;

public class OverdueTaskBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OverdueReviewOptions> _options;
    private readonly ILogger<OverdueTaskBackgroundService> _logger;

    public OverdueTaskBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OverdueReviewOptions> options,
        ILogger<OverdueTaskBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("Revision automatica de tareas vencidas deshabilitada (OverdueReview:Enabled=false).");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, options.IntervalSeconds));
        _logger.LogInformation("Revision automatica de tareas vencidas iniciada,el intervalo es: {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var review = scope.ServiceProvider.GetRequiredService<IOverdueTaskReviewService>();

            var count = await review.ReviewAndNotifyAsync(userId: null, stoppingToken);
            _logger.LogInformation("Revision terminada: {Count} tarea(s) notificada(s)", count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
    
            _logger.LogError(ex, "Hubo un error en la revision automatica de tareas vencidas");
        }
    }
}