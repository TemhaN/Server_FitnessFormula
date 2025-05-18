using FitnessFormula.Data;
using FitnessFormula.Models;
using FitnessFormula.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessFormula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkoutRegistrationsController : ControllerBase
    {
        private readonly FitnessDbContext _context;
        private readonly INotificationService _notificationService;

        public WorkoutRegistrationsController(FitnessDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<ActionResult<object>> RegisterForWorkout(int userId, int workoutId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var workout = await _context.Workouts
                .Include(w => w.WorkoutRegistrations)
                .Include(w => w.Trainer)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId);

            if (workout == null)
                return NotFound(new { message = "Тренировка не найдена" });

            var existingRegistration = await _context.WorkoutRegistrations
                .FirstOrDefaultAsync(r => r.UserId == userId && r.WorkoutId == workoutId);

            if (existingRegistration != null)
                return Conflict(new { message = "Вы уже зарегистрированы на эту тренировку" });

            if (workout.WorkoutRegistrations.Count >= workout.MaxParticipants)
                return BadRequest(new { message = "Тренировка переполнена. Нет доступных мест." });

            var registration = new WorkoutRegistration
            {
                UserId = userId,
                WorkoutId = workoutId,
                RegistrationDate = DateTime.UtcNow
            };

            _context.WorkoutRegistrations.Add(registration);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.SaveChangesAsync();

                var existingAttendance = await _context.WorkoutAttendance
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.WorkoutId == workoutId);

                if (existingAttendance != null)
                {
                    await transaction.RollbackAsync();
                    return Conflict(new { message = "Запись посещения для этой тренировки уже существует." });
                }

                var attendance = new WorkoutAttendance
                {
                    WorkoutId = workoutId,
                    UserId = userId,
                    AttendanceDate = DateTime.UtcNow
                };
                _context.WorkoutAttendance.Add(attendance);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                if (ex.InnerException?.Message.Contains("Тренировка переполнена") ?? false)
                    return BadRequest(new { message = "Тренировка переполнена. Нет доступных мест." });
                return StatusCode(500, new { message = $"Ошибка базы данных: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"Непредвиденная ошибка: {ex.Message}" });
            }

            try
            {
                await _notificationService.SendNotificationAsync(
                    userId,
                    "Регистрация на тренировку",
                    $"Вы успешно зарегистрированы на тренировку '{workout.Title}' в {workout.StartTime:HH:mm}.",
                    "Registration",
                    workoutId
                );

                if (workout.Trainer?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        workout.Trainer.UserId,
                        "Новая регистрация",
                        $"Пользователь {user.FullName} зарегистрировался на вашу тренировку '{workout.Title}' в {workout.StartTime:HH:mm}.",
                        "Registration",
                        workoutId
                    );
                }
            }
            catch (Exception) { }

            return Ok(new { message = "Вы успешно зарегистрированы на тренировку" });
        }

        [HttpDelete("{registrationId}/user/{userId}")]
        public async Task<IActionResult> CancelRegistration(int registrationId, int userId)
        {
            var registration = await _context.WorkoutRegistrations
                .Include(r => r.Workout)
                .ThenInclude(w => w.Trainer)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId && r.UserId == userId);

            if (registration == null)
                return NotFound(new { message = "Регистрация не найдена или не принадлежит пользователю." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            _context.WorkoutRegistrations.Remove(registration);

            var attendance = await _context.WorkoutAttendance
                .FirstOrDefaultAsync(a => a.WorkoutId == registration.WorkoutId && a.UserId == userId);
            if (attendance != null)
                _context.WorkoutAttendance.Remove(attendance);

            await _context.SaveChangesAsync();

            try
            {
                await _notificationService.SendNotificationAsync(
                    userId,
                    "Отмена регистрации",
                    $"Вы отменили регистрацию на тренировку '{registration.Workout.Title}'.",
                    "Cancellation",
                    registration.WorkoutId
                );

                if (registration.Workout.Trainer?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        registration.Workout.Trainer.UserId,
                        "Отмена регистрации",
                        $"Пользователь {user.FullName} отменил регистрацию на вашу тренировку '{registration.Workout.Title}' в {registration.Workout.StartTime:HH:mm}.",
                        "Cancellation",
                        registration.WorkoutId
                    );
                }
            }
            catch (Exception) { }

            return Ok(new { message = "Регистрация успешно отменена" });
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserRegistrations(int userId)
        {
            var registrations = await _context.WorkoutRegistrations
                .Where(r => r.UserId == userId)
                .Include(r => r.Workout)
                .ThenInclude(w => w.Gym)
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
                        r.Workout.ImageUrl,
                        r.Workout.MaxParticipants,
                        RegisteredCount = r.Workout.WorkoutRegistrations.Count,
                        AvailableSlots = r.Workout.MaxParticipants - r.Workout.WorkoutRegistrations.Count,
                        Gym = r.Workout.Gym != null ? new
                        {
                            r.Workout.Gym.GymId,
                            r.Workout.Gym.GymName,
                            r.Workout.Gym.Address
                        } : null
                    }
                })
                .ToListAsync();

            return Ok(registrations);
        }

        [HttpGet("workout/{workoutId}/trainer/{trainerId}")]
        public async Task<ActionResult<object>> GetWorkoutRegistrationsForTrainer(int workoutId, int trainerId)
        {
            // Проверяем, что тренировка существует и принадлежит тренеру
            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId && w.TrainerId == trainerId);

            if (workout == null)
            {
                return NotFound(new { message = "Тренировка не найдена или вы не являетесь её создателем." });
            }

            var registrations = await _context.WorkoutRegistrations
                .Where(r => r.WorkoutId == workoutId)
                .Include(r => r.User)
                .Select(r => new
                {
                    RegistrationId = r.RegistrationId,
                    RegistrationDate = r.RegistrationDate,
                    User = new
                    {
                        UserId = r.User.UserId,
                        FullName = r.User.FullName,
                        Email = r.User.Email,
                        PhoneNumber = r.User.PhoneNumber,
                        Avatar = r.User.Avatar
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                TotalUsers = registrations.Count,
                Registrations = registrations
            });
        }

        // Удаление зарегистрированного пользователя тренером
        [HttpDelete("workout/{workoutId}/trainer/{trainerId}/user/{userId}")]
        public async Task<IActionResult> RemoveUserFromWorkout(int workoutId, int trainerId, int userId)
        {
            // Проверяем, что тренировка существует и принадлежит тренеру
            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId && w.TrainerId == trainerId);

            if (workout == null)
            {
                return NotFound(new { message = "Тренировка не найдена или вы не являетесь её создателем." });
            }

            // Находим регистрацию
            var registration = await _context.WorkoutRegistrations
                .FirstOrDefaultAsync(r => r.WorkoutId == workoutId && r.UserId == userId);

            if (registration == null)
            {
                return NotFound(new { message = "Пользователь не зарегистрирован на эту тренировку." });
            }

            // Удаляем регистрацию
            _context.WorkoutRegistrations.Remove(registration);

            // Удаляем запись из WorkoutAttendance
            var attendance = await _context.WorkoutAttendance
                .FirstOrDefaultAsync(a => a.WorkoutId == workoutId && a.UserId == userId);
            if (attendance != null)
            {
                _context.WorkoutAttendance.Remove(attendance);
            }

            await _context.SaveChangesAsync();

            // Отправляем уведомление пользователю
            await _notificationService.SendNotificationAsync(
                userId,
                "Отмена регистрации тренером",
                $"Тренер отменил вашу регистрацию на тренировку '{workout.Title}'.",
                "CancellationByTrainer",
                workoutId
            );

            return Ok(new { message = "Пользователь успешно удалён из тренировки." });
        }

    }
}