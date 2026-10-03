namespace ToDoApi.Services.Notifications;

public interface IOverdueTaskNotifier
{
    Task NotifyAsync(OverdueTaskNotification notification, CancellationToken cancellationToken = default);
}