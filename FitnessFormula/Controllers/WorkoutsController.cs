using FitnessFormula.Data;
using FitnessFormula.Models;
using FitnessFormula.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Controllers
{
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
        public async Task<ActionResult<IEnumerable<object>>> GetWorkouts(
            [FromQuery] string? search,
            [FromQuery] int? trainerId,
            [FromQuery] int? gymId,
            [FromQuery] DateTime? startDate,
            [FromQuery] int? skillId)
        {
            var query = _context.Workouts
                .Include(w => w.Trainer)
                .ThenInclude(t => t.User)
                .Include(w => w.Gym)
                .Include(w => w.Trainer.TrainerSkills)
                .ThenInclude(ts => ts.Skill)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(w => w.Title.ToLower().Contains(search) || w.Description.ToLower().Contains(search));
            }

            if (trainerId.HasValue)
            {
                query = query.Where(w => w.TrainerId == trainerId.Value);
            }

            if (gymId.HasValue)
            {
                query = query.Where(w => w.GymId == gymId.Value);
            }

            if (startDate.HasValue)
            {
                var endDate = startDate.Value.AddDays(1);
                query = query.Where(w => w.StartTime >= startDate.Value && w.StartTime < endDate);
            }

            if (skillId.HasValue)
            {
                query = query.Where(w => w.Trainer.TrainerSkills.Any(ts => ts.SkillId == skillId.Value));
            }

            return await query
                .Select(w => new
                {
                    w.WorkoutId,
                    w.Title,
                    w.StartTime,
                    w.Description,
                    w.TrainerId,
                    w.ImageUrl,
                    w.MaxParticipants,
                    RegisteredCount = w.WorkoutRegistrations.Count,
                    AvailableSlots = w.MaxParticipants - w.WorkoutRegistrations.Count,
                    w.GymId,
                    Gym = w.Gym != null ? new
                    {
                        w.Gym.GymId,
                        w.Gym.GymName,
                        w.Gym.Address
                    } : null,
                    Trainer = w.Trainer != null ? new
                    {
                        w.Trainer.TrainerId,
                        w.Trainer.User.FullName,
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
                .ThenInclude(t => t.User)
                .Include(w => w.Gym)
                .Where(w => w.WorkoutId == id)
                .Select(w => new
                {
                    w.WorkoutId,
                    w.Title,
                    w.StartTime,
                    w.Description,
                    w.TrainerId,
                    w.ImageUrl,
                    w.MaxParticipants,
                    RegisteredCount = w.WorkoutRegistrations.Count,
                    AvailableSlots = w.MaxParticipants - w.WorkoutRegistrations.Count,
                    w.GymId,
                    Gym = w.Gym != null ? new
                    {
                        w.Gym.GymId,
                        w.Gym.GymName,
                        w.Gym.Address
                    } : null,
                    Trainer = w.Trainer != null ? new
                    {
                        w.Trainer.TrainerId,
                        w.Trainer.User.FullName,
                        w.Trainer.User.Avatar,
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
                .Include(w => w.Gym)
                .Select(w => new
                {
                    w.WorkoutId,
                    w.Title,
                    w.StartTime,
                    w.Description,
                    w.TrainerId,
                    w.ImageUrl,
                    w.MaxParticipants,
                    RegisteredCount = w.WorkoutRegistrations.Count,
                    AvailableSlots = w.MaxParticipants - w.WorkoutRegistrations.Count,
                    w.GymId,
                    Gym = w.Gym != null ? new
                    {
                        w.Gym.GymId,
                        w.Gym.GymName,
                        w.Gym.Address
                    } : null,
                    Trainer = new
                    {
                        w.Trainer.TrainerId,
                        w.Trainer.User.FullName,
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
        public async Task<ActionResult<object>> CreateWorkout([FromForm] WorkoutCreateRequest request)
        {
            var trainer = await _context.Trainers
                .Include(t => t.User) // Включаем связанный User
                .FirstOrDefaultAsync(t => t.TrainerId == request.TrainerId);

            if (trainer == null)
            {
                return NotFound(new { message = "Тренер не найден" });
            }

            if (request.ImageFile == null || request.ImageFile.Length == 0)
            {
                return BadRequest(new { message = "Файл изображения не был загружен." });
            }

            if (!request.GymId.HasValue)
            {
                return BadRequest(new { message = "Необходимо выбрать спортзал." });
            }

            var gym = await _context.Gyms.FindAsync(request.GymId);
            if (gym == null)
            {
                return NotFound(new { message = "Зал не найден" });
            }

            if (!request.MaxParticipants.HasValue || request.MaxParticipants <= 0)
            {
                return BadRequest(new { message = "Максимальное количество участников должно быть больше 0." });
            }

            var imagesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(imagesFolderPath))
            {
                Directory.CreateDirectory(imagesFolderPath);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ImageFile.FileName);
            var filePath = Path.Combine(imagesFolderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.ImageFile.CopyToAsync(stream);
            }

            var imageUrl = $"/images/{fileName}";
            var startTimeUtc = request.StartTime.ToUniversalTime();

            var workout = new Workout
            {
                Title = request.Title,
                StartTime = startTimeUtc,
                Description = request.Description,
                TrainerId = request.TrainerId,
                ImageUrl = imageUrl,
                MaxParticipants = request.MaxParticipants.Value,
                GymId = request.GymId.Value
            };

            _context.Workouts.Add(workout);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Ошибка при сохранении данных в базу данных." });
            }

            // Проецируем объект workout, чтобы избежать циклических ссылок
            var responseWorkout = new
            {
                workout.WorkoutId,
                workout.Title,
                workout.StartTime,
                workout.Description,
                workout.TrainerId,
                workout.ImageUrl,
                workout.MaxParticipants,
                RegisteredCount = 0, // Новая тренировка, регистраций пока нет
                AvailableSlots = workout.MaxParticipants, // Все места доступны
                workout.GymId,
                Gym = new
                {
                    gym.GymId,
                    gym.GymName,
                    gym.Address
                },
                Trainer = new
                {
                    trainer.TrainerId,
                    FullName = trainer.User?.FullName ?? "Не указано", // Безопасное обращение
                    trainer.Description,
                    trainer.ExperienceYears
                }
            };

            return CreatedAtAction(nameof(GetWorkoutById), new { id = workout.WorkoutId }, new
            {
                message = "Тренировка успешно создана",
                workout = responseWorkout
            });
        }

        [HttpDelete("{workoutId}/trainer/{trainerId}")]
        public async Task<IActionResult> DeleteWorkout(int workoutId, int trainerId, [FromServices] INotificationService notificationService)
        {
            var workout = await _context.Workouts
                .Include(w => w.WorkoutRegistrations)
                .ThenInclude(wr => wr.User)
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId && w.TrainerId == trainerId);

            if (workout == null)
            {
                return NotFound(new { message = $"Тренировка с ID {workoutId} не найдена или не принадлежит тренеру с ID {trainerId}." });
            }

            // Отправка уведомлений всем зарегистрированным пользователям
            foreach (var registration in workout.WorkoutRegistrations)
            {
                await notificationService.SendNotificationAsync(
                    registration.UserId,
                    "Тренировка отменена",
                    $"Тренировка '{workout.Title}' была отменена.",
                    "Cancellation",
                    workout.WorkoutId
                );
            }

            _context.Workouts.Remove(workout);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Тренировка успешно удалена" });
        }

        [HttpGet("{workoutId}/registrations/trainer/{trainerId}")]
        public async Task<ActionResult<object>> GetWorkoutRegistrations(int workoutId, int trainerId)
        {
            var workout = await _context.Workouts
                .Include(w => w.Gym)
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId && w.TrainerId == trainerId);

            if (workout == null)
            {
                return NotFound(new { message = $"Тренировка с ID {workoutId} не найдена или не принадлежит тренеру с ID {trainerId}." });
            }

            var registrations = await _context.WorkoutRegistrations
                .Where(wr => wr.WorkoutId == workoutId)
                .Include(wr => wr.User)
                .Select(wr => new
                {
                    RegistrationId = wr.RegistrationId,
                    User = new
                    {
                        UserId = wr.User.UserId,
                        FullName = wr.User.FullName,
                        Email = wr.User.Email,
                        PhoneNumber = wr.User.PhoneNumber,
                        Avatar = wr.User.Avatar,
                        RegistrationDate = wr.User.RegistrationDate
                    },
                    RegistrationDate = wr.RegistrationDate
                })
                .ToListAsync();

            var totalUsers = registrations.Count;

            return Ok(new
            {
                WorkoutId = workoutId,
                Title = workout.Title,
                StartTime = workout.StartTime,
                MaxParticipants = workout.MaxParticipants,
                RegisteredCount = totalUsers,
                AvailableSlots = workout.MaxParticipants - totalUsers,
                Gym = workout.Gym != null ? new
                {
                    workout.Gym.GymId,
                    workout.Gym.GymName,
                    workout.Gym.Address
                } : null,
                Registrations = registrations
            });
        }

        [HttpGet("daily-workout/{userId}")]
        public async Task<ActionResult<object>> GetDailyWorkout(int userId)
        {
            var userInterests = await _context.UserInterests
                .Where(ui => ui.UserId == userId)
                .Select(ui => ui.SkillId)
                .ToListAsync();

            var query = _context.Workouts
                .Include(w => w.Trainer)
                .ThenInclude(t => t.User)
                .Include(w => w.Gym)
                .Include(w => w.Trainer.TrainerSkills)
                .Where(w => w.StartTime.Date == DateTime.UtcNow.Date);

            if (userInterests.Any())
            {
                query = query.Where(w => w.Trainer.TrainerSkills.Any(ts => userInterests.Contains(ts.SkillId)));
            }

            var workouts = await query
                .Select(w => new
                {
                    w.WorkoutId,
                    w.Title,
                    w.StartTime,
                    w.Description,
                    w.TrainerId,
                    w.ImageUrl,
                    w.MaxParticipants,
                    RegisteredCount = w.WorkoutRegistrations.Count,
                    AvailableSlots = w.MaxParticipants - w.WorkoutRegistrations.Count,
                    w.GymId,
                    Gym = w.Gym != null ? new
                    {
                        w.Gym.GymId,
                        w.Gym.GymName,
                        w.Gym.Address
                    } : null,
                    Trainer = w.Trainer != null ? new
                    {
                        w.Trainer.TrainerId,
                        w.Trainer.User.FullName,
                        w.Trainer.Description,
                        w.Trainer.ExperienceYears
                    } : null
                })
                .ToListAsync();

            if (!workouts.Any())
            {
                return NotFound(new { message = "Занятие дня не найдено." });
            }

            var random = new Random();
            var dailyWorkout = workouts[random.Next(workouts.Count)];

            return Ok(dailyWorkout);
        }

        public class WorkoutCreateRequest
        {
            [Required]
            public string Title { get; set; }

            [Required]
            public DateTime StartTime { get; set; }

            [Required]
            public string Description { get; set; }

            [Required]
            public int TrainerId { get; set; }

            [Required]
            public IFormFile ImageFile { get; set; }

            public int? MaxParticipants { get; set; }

            public int? GymId { get; set; }
        }
    }
}