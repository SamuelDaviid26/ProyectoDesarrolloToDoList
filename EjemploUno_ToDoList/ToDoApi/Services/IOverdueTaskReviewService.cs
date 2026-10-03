namespace ToDoApi.Services;

public interface IOverdueTaskReviewService
{
    /// <param name="userId">Si se indica, solo revisa las tareas de ese usuario; si es null, las de todos.</param>
    /// <returns>Cantidad de tareas notificadas en esta ejecución.</returns>
    Task<int> ReviewAndNotifyAsync(string? userId, CancellationToken cancellationToken = default);
}