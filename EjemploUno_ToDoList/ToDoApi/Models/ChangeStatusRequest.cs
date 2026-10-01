namespace ToDoApi.Models
{
    public class ChangeStatusRequest
    {
            public ToDoStatus Status { get; set; }

       public bool ForzarCompletado { get; set; } = false;

    }
}