using System.Globalization;

namespace ToDoApi.Services.Notifications;


public sealed class FileOverdueTaskNotifier : IOverdueTaskNotifier
{
    private readonly string _filePath;
    private readonly ILogger<FileOverdueTaskNotifier> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public FileOverdueTaskNotifier(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<FileOverdueTaskNotifier> logger)
    {
        _logger = logger;

        var configured = configuration["Notifications:LogFilePath"];
        if (string.IsNullOrWhiteSpace(configured))
            configured = Path.Combine("logs", "overdue-notifications.log");

        _filePath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public async Task NotifyAsync(OverdueTaskNotification n, CancellationToken cancellationToken = default)
    {
        var overdueBy = n.DetectedAtUtc - n.DueDateUtc;

        var line = string.Create(CultureInfo.InvariantCulture,
            $"{n.DetectedAtUtc:O} | TAREA VENCIDA | TaskId={n.TaskId} | UserId={n.UserId ?? "(sin dueño)"} | " +
            $"Title=\"{Sanitize(n.Title)}\" | DueDate={n.DueDateUtc:O} | VencidaHace={overdueBy:d\\.hh\\:mm\\:ss}");

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_filePath, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }

        _logger.LogWarning(
            "Tarea vencida {TaskId} del usuario {UserId}: \"{Title}\" (vencía {DueDate:O})",
            n.TaskId, n.UserId, Sanitize(n.Title), n.DueDateUtc);
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Select(c => char.IsControl(c) ? ' ' : c));
}