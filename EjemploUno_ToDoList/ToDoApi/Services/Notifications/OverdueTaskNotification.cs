namespace ToDoApi.Services.Notifications;

public sealed record OverdueTaskNotification(
    int TaskId,
    string? UserId,
    string Title,
    DateTime DueDateUtc,
    DateTime DetectedAtUtc);