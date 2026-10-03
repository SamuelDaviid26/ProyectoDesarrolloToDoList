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


        private static readonly Dictionary<ToDoStatus, ToDoStatus[]> AllowedTransitions = new()
        {
            [ToDoStatus.Pendiente] = new[] { ToDoStatus.EnProgreso, ToDoStatus.Cancelada },
            [ToDoStatus.EnProgreso] = new[] { ToDoStatus.Completada, ToDoStatus.Cancelada },
            [ToDoStatus.Completada] = Array.Empty<ToDoStatus>(),
            [ToDoStatus.Cancelada] = Array.Empty<ToDoStatus>()
        };
        private string? GetCurrentUserId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetStatistics()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var now = DateTime.UtcNow;
            var tasks = await _context.ToDoItems
                .AsNoTracking()
                .Where(t => t.UserId == currentUserId)
                .Select(t => new { t.Status, t.DueDate, t.CreatedAt, t.CompletedAt })
                .ToListAsync();

            var countsByStatus = tasks
                .GroupBy(t => t.Status)
                .ToDictionary(group => group.Key, group => group.Count());
            var byStatus = Enum.GetValues<ToDoStatus>()
                .ToDictionary(
                    status => status.ToString(),
                    status => countsByStatus.GetValueOrDefault(status));

            var overdue = tasks.Count(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < now &&
                t.Status != ToDoStatus.Completada &&
                t.Status != ToDoStatus.Cancelada);

            var completionDurationsInDays = tasks
                .Where(t => t.Status == ToDoStatus.Completada && t.CompletedAt.HasValue)
                .Select(t =>
                {
                    var createdAtUtc = NormalizeCreatedAtToUtc(t.CreatedAt);
                    var completedAtUtc = NormalizeCompletedAtToUtc(t.CompletedAt!.Value);
                    return (completedAtUtc - createdAtUtc).TotalDays;
                })
                .Where(durationInDays => durationInDays >= 0)
                .ToList();

            double? averageCompletionDays = completionDurationsInDays.Count == 0
                ? null
                : completionDurationsInDays.Average();

            return Ok(new
            {
                total = tasks.Count,
                byStatus,
                overdue,
                averageCompletionDays
            });
        }

        private static DateTime NormalizeCreatedAtToUtc(DateTime createdAt)
        {
            return createdAt.Kind switch
            {
                DateTimeKind.Utc => createdAt,
                DateTimeKind.Local => createdAt.ToUniversalTime(),
                _ => TimeZoneInfo.ConvertTimeToUtc(createdAt, TimeZoneInfo.Local)
            };
        }

        private static DateTime NormalizeCompletedAtToUtc(DateTime completedAt)
        {
            return completedAt.Kind switch
            {
                DateTimeKind.Utc => completedAt,
                DateTimeKind.Local => completedAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(completedAt, DateTimeKind.Utc)
            };
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
        public async Task<ActionResult<IEnumerable<ToDoItem>>> GetToDoItems(
            [FromQuery] ToDoStatus? status,
            [FromQuery] bool? overdue)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var query = _context.ToDoItems
                .Where(t => t.UserId == currentUserId)
                .AsQueryable();

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
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            todoItem.UserId = currentUserId;
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
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var todoItem = await _context.ToDoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

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
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var todoItem = await _context.ToDoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);

            if (todoItem == null) return NotFound();

            //Validar la transición de estado, con ayuda del diccionario AllowedTransitions
            if (!AllowedTransitions[todoItem.Status].Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = $"Transición no permitida: {todoItem.Status} -> {request.Status}."
                });
            }


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