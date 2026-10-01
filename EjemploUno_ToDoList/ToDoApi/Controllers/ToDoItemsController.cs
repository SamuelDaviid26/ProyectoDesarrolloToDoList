using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoApi.Data;
using ToDoApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

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

        [HttpGet("{id}")]
        public async Task<ActionResult<ToDoItem>> GetToDoItem(int id)
        {
            var todoItem = await _context.ToDoItems.FindAsync(id);

            if (todoItem == null) return NotFound();

            return Ok(todoItem);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToDoItem>>> GetToDoItems(
            [FromQuery] ToDoStatus? status,
            [FromQuery] bool? overdue)
        {
            var query = _context.ToDoItems.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            // tareas encidas que todavía no llegaron a un estado final
            if (overdue == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t =>
                    t.DueDate != null &&
                    t.DueDate < now &&
                    t.Status != ToDoStatus.Completada &&
                    t.Status != ToDoStatus.Cancelada);
            }

            return Ok(await query.ToListAsync());
        }

        [HttpPost]
        public async Task<ActionResult<ToDoItem>> CreateToDoItem(ToDoItem todoItem)
        {
            // Las tareas estan en pendienter cuando se crea
            todoItem.Status = ToDoStatus.Pendiente;
            todoItem.CompletedAt = null;

            _context.ToDoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetToDoItem), new { id = todoItem.Id }, todoItem);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateToDoItem(int id, ToDoItem updated)
        {
            var todoItem = await _context.ToDoItems.FindAsync(id);

            if (todoItem == null) return NotFound();

            todoItem.Title = updated.Title;
            todoItem.Description = updated.Description;
            todoItem.DueDate = updated.DueDate;


            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ToDoItem>> ChangeStatus(int id, ChangeStatusRequest request)
        {
            var todoItem = await _context.ToDoItems.FindAsync(id);

            if (todoItem == null) return NotFound();


            // bloquear Completada si ya venció
            if (request.Status == ToDoStatus.Completada
                && todoItem.DueDate.HasValue
                && todoItem.DueDate.Value < DateTime.UtcNow
                && !request.ForzarCompletado)
            {
                return BadRequest(new
                {
                    message = "The task is overdue and cannot be completed. Set 'forceComplete' to true to complete it anyway."
                });
            }

            todoItem.Status = request.Status;
            todoItem.CompletedAt = request.Status == ToDoStatus.Completada ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();

            return Ok(todoItem);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteToDoItem(int id)
        {
            var todoItem = await _context.ToDoItems.FindAsync(id);

            if (todoItem == null) return NotFound();

            _context.ToDoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}