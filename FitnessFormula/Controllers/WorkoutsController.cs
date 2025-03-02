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
        public async Task<ActionResult<object>> CreateWorkout([FromForm] WorkoutCreateRequest request)
        {
            // Проверяем, существует ли тренер
            var trainer = await _context.Trainers.FindAsync(request.TrainerId);
            if (trainer == null)
            {
                return NotFound(new { message = "Тренер не найден" });
            }

            // Проверяем, что файл изображения был передан
            if (request.ImageFile == null || request.ImageFile.Length == 0)
            {
                return BadRequest(new { message = "Файл изображения не был загружен." });
            }

            // Путь к папке wwwroot/images
            var imagesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            // Если папка не существует, создаем её
            if (!Directory.Exists(imagesFolderPath))
            {
                Directory.CreateDirectory(imagesFolderPath);
            }

            // Генерируем уникальное имя файла
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ImageFile.FileName);

            // Полный путь для сохранения файла
            var filePath = Path.Combine(imagesFolderPath, fileName);

            // Сохраняем файл на сервере
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.ImageFile.CopyToAsync(stream);
            }

            // Генерируем URL для доступа к файлу
            var imageUrl = $"/images/{fileName}";

            // Преобразуем StartTime в UTC
            var startTimeUtc = request.StartTime.ToUniversalTime();

            // Создаем новый объект Workout
            var workout = new Workout
            {
                Title = request.Title,
                StartTime = startTimeUtc,
                Description = request.Description,
                TrainerId = request.TrainerId,
                ImageUrl = imageUrl // Сохраняем URL изображения
            };

            // Добавляем workout в контекст
            _context.Workouts.Add(workout);

            // Сохраняем изменения в базе данных
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Логируем ошибку
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Ошибка при сохранении данных в базу данных." });
            }

            // Возвращаем успешный результат
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
            public IFormFile ImageFile { get; set; } // Файл изображения
        }
    }
}
