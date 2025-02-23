using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessFormula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkoutRegistrationsController : ControllerBase
    {
        private readonly FitnessDbContext _context;

        public WorkoutRegistrationsController(FitnessDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<object>> RegisterForWorkout(int userId, int workoutId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var workout = await _context.Workouts.FindAsync(workoutId);
            if (workout == null)
                return NotFound(new { message = "Тренировка не найдена" });

            var existingRegistration = await _context.WorkoutRegistrations
                .FirstOrDefaultAsync(r => r.UserId == userId && r.WorkoutId == workoutId);

            if (existingRegistration != null)
                return Conflict(new { message = "Вы уже зарегистрированы на эту тренировку" });

            var registration = new WorkoutRegistration
            {
                UserId = userId,
                WorkoutId = workoutId,
                RegistrationDate = DateTime.UtcNow
            };

            _context.WorkoutRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Вы успешно зарегистрированы на тренировку" });
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserRegistrations(int userId)
        {
            var registrations = await _context.WorkoutRegistrations
                .Where(r => r.UserId == userId)
                .Include(r => r.Workout)
                .Select(r => new
                {
                    r.RegistrationId,
                    r.RegistrationDate,
                    Workout = new
                    {
                        r.Workout.WorkoutId,
                        r.Workout.Title,
                        r.Workout.StartTime,
                        r.Workout.Description,
                        r.Workout.ImageUrl
                    }
                })
                .ToListAsync();

            return Ok(registrations);
        }
    }
}
