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

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetWorkoutById(int id)
        {
            var workout = await _context.Workouts
                .Include(w => w.Trainer)
                .Where(w => w.WorkoutId == id)
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
                        w.Trainer.User.Avatar,
                        w.Trainer.User.FullName,
                        w.Trainer.User.PhoneNumber,
                        w.Trainer.Description,
                        w.Trainer.ExperienceYears
                    } : null
                })
                .FirstOrDefaultAsync();

            if (workout == null)
            {
                return NotFound($"Тренировка с ID {id} не найдена.");
            }

            return workout;
        }


        [HttpGet("trainer/{trainerId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetWorkoutsByTrainer(int trainerId)
        {
            var workouts = await _context.Workouts
                .Where(w => w.TrainerId == trainerId)
                .Select(w => new
                {
                    w.WorkoutId,
                    w.Title,
                    w.StartTime,
                    w.Description,
                    w.TrainerId,
                    w.ImageUrl,
                    Trainer = new
                    {
                        w.Trainer.TrainerId,
                        w.Trainer.Description,
                        w.Trainer.ExperienceYears
                    }
                })
                .ToListAsync();

            if (!workouts.Any())
            {
                return NotFound($"Нет тренировок для тренера с ID {trainerId}.");
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
