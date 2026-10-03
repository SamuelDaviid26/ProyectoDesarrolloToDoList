using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToDoApi.Services;

namespace ToDoApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/todoitems")]
    public class OverdueNotificationsController : ControllerBase
    {
        private readonly IOverdueTaskReviewService _review;

        public OverdueNotificationsController(IOverdueTaskReviewService review)
        {
            _review = review;
        }

        [HttpPost("notificarvencidas")]
        public async Task<ActionResult> NotificarVencidas(CancellationToken cancellationToken)
        {
            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

            var notificadas = await _review.ReviewAndNotifyAsync(currentUserId, cancellationToken);

            return Ok(new { notificadas });
        }
    }
}