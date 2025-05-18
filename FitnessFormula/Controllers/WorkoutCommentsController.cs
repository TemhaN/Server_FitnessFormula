using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkoutCommentsController : ControllerBase
    {
        private readonly FitnessDbContext _context;

        public WorkoutCommentsController(FitnessDbContext context)
        {
            _context = context;
        }

        [HttpGet("workout/{workoutId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetCommentsByWorkout(int workoutId)
        {
            var comments = await _context.WorkoutComments
                .Where(c => c.WorkoutId == workoutId)
                .Include(c => c.User)
                .Select(c => new
                {
                    c.CommentId,
                    c.WorkoutId,
                    c.UserId,
                    User = new
                    {
                        c.User.UserId,
                        c.User.FullName,
                        c.User.Avatar
                    },
                    c.CommentText,
                    c.CommentDate
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateComment([FromBody] WorkoutCommentRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var workout = await _context.Workouts.FindAsync(request.WorkoutId);
            if (workout == null)
                return NotFound(new { message = "Тренировка не найдена" });

            var comment = new WorkoutComment
            {
                WorkoutId = request.WorkoutId,
                UserId = request.UserId,
                CommentText = request.CommentText,
                CommentDate = DateTime.UtcNow
            };

            _context.WorkoutComments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCommentsByWorkout), new { workoutId = comment.WorkoutId }, new
            {
                message = "Комментарий успешно добавлен",
                comment
            });
        }

        [HttpDelete("{commentId}/user/{userId}")]
        public async Task<IActionResult> DeleteComment(int commentId, int userId)
        {
            var comment = await _context.WorkoutComments
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.UserId == userId);

            if (comment == null)
            {
                return NotFound(new { message = "Комментарий не найден или не принадлежит пользователю." });
            }

            _context.WorkoutComments.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Комментарий успешно удалён" });
        }
    }

    public class WorkoutCommentRequest
    {
        [Required]
        public int WorkoutId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string CommentText { get; set; }
    }
}