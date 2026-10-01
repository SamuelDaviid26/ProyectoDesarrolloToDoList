using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Models
{

public class ToDoItem
    {

[Key]
public int Id{get;set;}

[Required]
[MaxLength(200)]
public string Title {get;set;}

[MaxLength(1000)]
public string Description {get;set;}

public DateTime CreatedAt {get;set;} = DateTime.Now;

public DateTime? CompletedAt {get;set;}

public int? CategoryId {get;set;}
public Category? Category {get;set;}

public ToDoStatus Status {get;set;} = ToDoStatus.Pendiente;
public DateTime? DueDate { get; set; }   
    
    }   


}