using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessFormula.Controllers
{
    // Контроллер тренировок
    [ApiController]
    [Route("api/[controller]")]
    public class WorkoutsController : ControllerBase
    {
        private readonly FitnessDbContext _context;

        public WorkoutsController(FitnessDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetWorkouts()
        {
            return await _context.Workouts
                .Include(w => w.Trainer)
                .Select(w => new
                {
                    w.WorkoutId,
                    w.Title,
                    w.StartTime,
                    w.Description,
                    w.TrainerId,
                    w.ImageUrl,
                    Trainer = w.Trainer != null ? new
                    {
                        w.Trainer.TrainerId,
                        w.Trainer.Description,
                        w.Trainer.ExperienceYears
                    } : null
                })
                .ToListAsync();
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetWorkoutsByUser(int userId)
        {
            var workouts = await _context.WorkoutRegistrations
                .Where(wr => wr.UserId == userId)
                .Include(wr => wr.Workout)
                .ThenInclude(w => w.Trainer)
                .Select(wr => new
                {
                    wr.Workout.WorkoutId,
                    wr.Workout.Title,
                    wr.Workout.StartTime,
                    wr.Workout.Description,
                    wr.Workout.TrainerId,
                    wr.Workout.ImageUrl,
                    Trainer = wr.Workout.Trainer != null ? new
                    {
                        wr.Workout.Trainer.TrainerId,
                        wr.Workout.Trainer.Description,
                        wr.Workout.Trainer.ExperienceYears
                    } : null
                })
                .ToListAsync();

            if (!workouts.Any())
            {
                return NotFound($"Нет тренировок, на которые подписан пользователь с ID {userId}.");
            }

            return workouts;
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateWorkout([FromBody] WorkoutCreateRequest request)
        {
            var trainer = await _context.Trainers.FindAsync(request.TrainerId);
            if (trainer == null)
            {
                return NotFound(new { message = "Тренер не найден" });
            }

            var workout = new Workout
            {
                Title = request.Title,
                StartTime = request.StartTime,
                Description = request.Description,
                TrainerId = request.TrainerId,
                ImageUrl = request.ImageUrl
            };

            _context.Workouts.Add(workout);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWorkouts), new { id = workout.WorkoutId }, new
            {
                message = "Тренировка успешно создана",
                workout
            });
        }

        public class WorkoutCreateRequest
        {
            public string Title { get; set; }
            public DateTime StartTime { get; set; }
            public string Description { get; set; }
            public int TrainerId { get; set; }
            public string ImageUrl { get; set; }
        }
    }
}
