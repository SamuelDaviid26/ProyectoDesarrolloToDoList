using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using ToDoApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ToDoApi.Data;

public class ToDoDbContext : IdentityDbContext<IdentityUser>
{

  public ToDoDbContext(DbContextOptions<ToDoDbContext>options) :base(options)  
    {}

    public DbSet<ToDoItem> ToDoItems {get;set;}

     public DbSet<Category> Categories => Set<Category>();
}
