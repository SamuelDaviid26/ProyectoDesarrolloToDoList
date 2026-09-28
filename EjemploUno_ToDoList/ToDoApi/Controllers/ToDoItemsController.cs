using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoApi.Data;
using ToDoApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ToDoItemsController : ControllerBase
    {
        private readonly ToDoDbContext _context;

        public ToDoItemsController(ToDoDbContext context)
        {
            _context = context;
        }

        private string? GetCurrentUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ToDoItem>> GetToDoItem(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var todoItem = await _context.ToDoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

            if (todoItem == null) return NotFound();

            return Ok(todoItem);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToDoItem>>> GetToDoItems([FromQuery] bool? completed)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var query = _context.ToDoItems
                .Where(t => t.UserId == currentUserId)
                .AsQueryable();

            if (completed.HasValue)
            {
                query = query.Where(t => t.isCompleted == completed.Value);
            }

            return Ok(await query.ToListAsync());
        }

        [HttpPost]
        public async Task<ActionResult<ToDoItem>> CreateToDoItem(ToDoItem todoItem)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            todoItem.UserId = currentUserId;

            _context.ToDoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetToDoItem), new { id = todoItem.Id }, todoItem);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateToDoItem(int id, ToDoItem updated)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var todoItem = await _context.ToDoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

            if (todoItem == null) return NotFound();

            todoItem.Title = updated.Title;
            todoItem.Description = updated.Description;
            todoItem.isCompleted = updated.isCompleted;
            todoItem.CompletedAt = updated.isCompleted ? DateTime.Now : null;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/toggle")]
        public async Task<ActionResult<ToDoItem>> ToggleToDoItem(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var todoItem = await _context.ToDoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

            if (todoItem == null) return NotFound();

            todoItem.isCompleted = !todoItem.isCompleted;
            todoItem.CompletedAt = todoItem.isCompleted ? DateTime.Now : null;

            await _context.SaveChangesAsync();

            return Ok(todoItem);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteToDoItem(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var todoItem = await _context.ToDoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

            if (todoItem == null) return NotFound();

            _context.ToDoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}