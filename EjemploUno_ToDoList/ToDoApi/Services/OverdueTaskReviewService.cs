using Microsoft.EntityFrameworkCore;
using ToDoApi.Data;
using ToDoApi.Models;
using ToDoApi.Services.Notifications;

namespace ToDoApi.Services;

public class OverdueTaskReviewService : IOverdueTaskReviewService
{
    private const int BatchSize = 200;

    private readonly ToDoDbContext _context;
    private readonly IOverdueTaskNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<OverdueTaskReviewService> _logger;

    public OverdueTaskReviewService(
        ToDoDbContext context,
        IOverdueTaskNotifier notifier,
        TimeProvider time,
        ILogger<OverdueTaskReviewService> logger)
    {
        _context = context;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    public async Task<int> ReviewAndNotifyAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var notified = 0;
        var lastId = 0;

        while (true)
        {

            var query = _context.ToDoItems
                .AsNoTracking()
                .Where(t => t.Id > lastId
                    && t.DueDate != null
                    && t.DueDate < now
                    && t.Status != ToDoStatus.Completada
                    && t.Status != ToDoStatus.Cancelada
                    && (t.NotifiedForDueDate == null || t.NotifiedForDueDate != t.DueDate));

            if (userId != null)
                query = query.Where(t => t.UserId == userId);

            var batch = await query
                .OrderBy(t => t.Id)
                .Take(BatchSize)
                .Select(t => new Candidate(t.Id, t.UserId, t.Title, t.DueDate!.Value, t.NotifiedForDueDate))
                .ToListAsync(cancellationToken);

            if (batch.Count == 0) break;
            lastId = batch[^1].Id;

            foreach (var task in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var claimed = await _context.ToDoItems
                    .Where(t => t.Id == task.Id
                        && t.DueDate == task.DueDate
                        && t.DueDate < now
                        && t.Status != ToDoStatus.Completada
                        && t.Status != ToDoStatus.Cancelada
                        && (t.NotifiedForDueDate == null || t.NotifiedForDueDate != t.DueDate))
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.NotifiedForDueDate, t => t.DueDate), cancellationToken);

                if (claimed == 0) continue;

                
                try
                {
                    await _notifier.NotifyAsync(
                        new OverdueTaskNotification(task.Id, task.UserId, task.Title,
                            DateTime.SpecifyKind(task.DueDate, DateTimeKind.Utc), now),
                        cancellationToken);
                    notified++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo notificar la tarea {TaskId}; se reintentará en la próxima revisión.", task.Id);

                    await _context.ToDoItems
                        .Where(t => t.Id == task.Id && t.NotifiedForDueDate == task.DueDate)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.NotifiedForDueDate, task.PreviousNotifiedDueDate),
                            CancellationToken.None);

                    if (ex is OperationCanceledException) throw;
                }
            }
        }

        return notified;
    }

    private sealed record Candidate(int Id, string? UserId, string Title, DateTime DueDate, DateTime? PreviousNotifiedDueDate);
}