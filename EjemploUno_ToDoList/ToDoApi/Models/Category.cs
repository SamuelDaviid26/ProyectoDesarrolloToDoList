using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ToDoApi.Models
{

public class Category
    {

[Key]
public int Id{get;set;}

[Required]
[MaxLength(100)]
public string Name {get;set;}

    }


}