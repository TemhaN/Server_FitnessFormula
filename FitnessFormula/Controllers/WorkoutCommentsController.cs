using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static FitnessFormula.Controllers.TrainersController;

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
        public async Task<ActionResult<IEnumerable<WorkoutCommentDto>>> GetCommentsByWorkout(int workoutId)
        {
            var comments = await _context.WorkoutComments
                .Include(c => c.User)
                .Where(c => c.WorkoutId == workoutId && c.IsApproved)
                .Select(c => new WorkoutCommentDto
                {
                    CommentId = c.CommentId,
                    WorkoutId = c.WorkoutId,
                    UserId = c.UserId,
                    CommentText = c.CommentText,
                    CommentDate = c.CommentDate,
                    IsApproved = c.IsApproved,
                    User = new UserDto
                    {
                        UserId = c.UserId,
                        FullName = c.User.FullName,
                        Avatar = c.User.Avatar
                    }
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

            // Проверка посещения занятия
            var attendance = await _context.WorkoutAttendance
                .FirstOrDefaultAsync(a => a.WorkoutId == request.WorkoutId && a.UserId == request.UserId);
            if (attendance == null)
                return BadRequest(new { message = "Вы не посещали эту тренировку, комментарий оставить нельзя." });

            // Проверка времени тренировки
            if (workout.StartTime > DateTime.UtcNow)
                return BadRequest(new { message = "Нельзя комментировать тренировку, которая еще не состоялась." });

            var comment = new WorkoutComment
            {
                WorkoutId = request.WorkoutId,
                UserId = request.UserId,
                CommentText = request.CommentText,
                CommentDate = DateTime.UtcNow,
                IsApproved = false
            };

            _context.WorkoutComments.Add(comment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCommentsByWorkout), new { workoutId = comment.WorkoutId }, new
            {
                message = "Комментарий отправлен на модерацию",
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

        [HttpPut("approve/{commentId}/trainer/{trainerId}")]
        public async Task<IActionResult> ApproveComment(int commentId, int trainerId)
        {
            var comment = await _context.WorkoutComments
                .Include(c => c.Workout)
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.Workout.TrainerId == trainerId);

            if (comment == null)
            {
                return NotFound(new { message = "Комментарий не найден или вы не являетесь тренером этой тренировки." });
            }

            comment.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Комментарий успешно одобрен" });
        }

        [HttpDelete("reject/{commentId}/trainer/{trainerId}")]
        public async Task<IActionResult> RejectComment(int commentId, int trainerId)
        {
            var comment = await _context.WorkoutComments
                .Include(c => c.Workout)
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.Workout.TrainerId == trainerId);

            if (comment == null)
            {
                return NotFound(new { message = "Комментарий не найден или вы не являетесь тренером этой тренировки." });
            }

            _context.WorkoutComments.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Комментарий отклонён и удалён" });
        }
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetUserComments(int userId)
        {
            var comments = await _context.WorkoutComments
                .Include(c => c.Workout)
                .ThenInclude(w => w.Trainer)
                .ThenInclude(t => t.User)
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    WorkoutId = c.WorkoutId,
                    UserId = c.UserId,
                    CommentText = c.CommentText,
                    CommentDate = c.CommentDate,
                    IsApproved = c.IsApproved,
                    WorkoutTitle = c.Workout.Title,
                    Trainer = c.Workout.Trainer != null ? new
                    {
                        FullName = c.Workout.Trainer.User.FullName
                    } : null
                })
                .ToListAsync();

            return Ok(comments);
        }
        [HttpGet("pending/trainer/{trainerId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetPendingWorkoutCommentsForTrainer(int trainerId)
        {
            var comments = await _context.WorkoutComments
              .Include(c => c.Workout)
              .Include(c => c.User)
              .Where(c => c.Workout.TrainerId == trainerId && !c.IsApproved)
              .Select(c => new CommentDto
              {
                  CommentId = c.CommentId,
                  WorkoutId = c.WorkoutId,
                  UserId = c.UserId,
                  CommentText = c.CommentText,
                  CommentDate = c.CommentDate,
                  IsApproved = c.IsApproved,
                  WorkoutTitle = c.Workout.Title,
                  User = new UserDto
                  {
                      UserId = c.UserId,
                      FullName = c.User.FullName,
                      Avatar = c.User.Avatar
                  }
              })
              .ToListAsync();

            return Ok(comments);
        }
    [HttpGet("pending/workout/{workoutId}/trainer/{trainerId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetPendingCommentsForWorkout(int workoutId, int trainerId)
        {
            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId && w.TrainerId == trainerId);

            if (workout == null)
            {
                return NotFound(new { message = "Тренировка не найдена или вы не являетесь её тренером." });
            }

            var comments = await _context.WorkoutComments
                .Include(c => c.Workout)
                .Include(c => c.User)
                .Where(c => c.WorkoutId == workoutId && !c.IsApproved)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    WorkoutId = c.WorkoutId,
                    UserId = c.UserId,
                    CommentText = c.CommentText,
                    CommentDate = c.CommentDate,
                    IsApproved = c.IsApproved,
                    WorkoutTitle = c.Workout.Title,
                    User = new UserDto
                    {
                        UserId = c.UserId,
                        FullName = c.User.FullName,
                        Avatar = c.User.Avatar
                    }
                })
                .ToListAsync();

            return Ok(comments);
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
    public class CommentDto
    {
        public int CommentId { get; set; }
        public int WorkoutId { get; set; }
        public int UserId { get; set; }
        public string CommentText { get; set; }
        public DateTime CommentDate { get; set; }
        public bool IsApproved { get; set; }
        public string WorkoutTitle { get; set; }
        public object Trainer { get; set; }
        public UserDto User { get; set; }
    }
    public class WorkoutCommentDto
    {
        public int CommentId { get; set; }
        public int WorkoutId { get; set; }
        public int UserId { get; set; }
        public string CommentText { get; set; }
        public DateTime CommentDate { get; set; }
        public bool IsApproved { get; set; } // Добавляем поле
        public UserDto User { get; set; }
    }
}