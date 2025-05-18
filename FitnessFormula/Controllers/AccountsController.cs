using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Globalization;

namespace FitnessFormula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly FitnessDbContext _context;

        public AccountsController(FitnessDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            return await _context.Users
                .Select(user => new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Avatar,
                    user.RegistrationDate
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.UserId == id)
                .Select(user => new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Avatar,
                    user.RegistrationDate
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return user;
        }

        [HttpPost("register")]
        public async Task<ActionResult<object>> Register(UserRegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Пароль обязателен." });

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
                return Conflict(new { message = "Пользователь с таким email уже существует." });

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Avatar = request.Avatar,
                RegistrationDate = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var session = await CreateUserSession(user.UserId);
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, new
            {
                message = "Пользователь успешно создан",
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Avatar,
                    user.RegistrationDate
                },
                session = new
                {
                    session.Token,
                    session.ExpiresAt
                }
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<object>> Login(UserLoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный email или пароль" });

            var session = await CreateUserSession(user.UserId);

            var trainer = await _context.Trainers
                .Where(t => t.UserId == user.UserId)
                .Include(t => t.TrainerSkills)
                .ThenInclude(ts => ts.Skill)
                .FirstOrDefaultAsync();

            var isTrainer = trainer != null;

            return Ok(new
            {
                message = "Успешный вход",
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Avatar,
                    user.RegistrationDate,
                    isTrainer,
                    trainer = isTrainer ? new
                    {
                        trainer.TrainerId,
                        trainer.Description,
                        trainer.ExperienceYears,
                        Skills = trainer.TrainerSkills.Select(ts => ts.Skill.SkillName).ToList()
                    } : null
                },
                session = new
                {
                    session.Token,
                    session.ExpiresAt
                }
            });
        }

        [HttpPut("update/{id}")]
        public async Task<ActionResult<object>> UpdateUser(int id, [FromForm] UserUpdateRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден." });
            }

            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.UserId != id);
                if (existingUser != null)
                {
                    return Conflict(new { message = "Пользователь с таким email уже существует." });
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            if (request.AvatarFile != null && request.AvatarFile.Length > 0)
            {
                var imagesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(imagesFolderPath))
                {
                    Directory.CreateDirectory(imagesFolderPath);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.AvatarFile.FileName);
                var filePath = Path.Combine(imagesFolderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.AvatarFile.CopyToAsync(stream);
                }

                user.Avatar = $"/images/{fileName}";
            }

            _context.Users.Update(user);
            _context.Entry(user).Property(u => u.RegistrationDate).IsModified = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Данные пользователя успешно обновлены",
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Avatar,
                    user.RegistrationDate
                }
            });
        }



        [HttpPost("interests/{userId}")]
        public async Task<IActionResult> UpdateUserInterests(int userId, [FromBody] List<int> skillIds)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var existingInterests = await _context.UserInterests
                .Where(ui => ui.UserId == userId)
                .ToListAsync();
            _context.UserInterests.RemoveRange(existingInterests);

            var validSkillIds = await _context.Skills
                .Where(s => skillIds.Contains(s.SkillId))
                .Select(s => s.SkillId)
                .ToListAsync();

            var newInterests = validSkillIds.Select(skillId => new UserInterest
            {
                UserId = userId,
                SkillId = skillId
            }).ToList();

            _context.UserInterests.AddRange(newInterests);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Интересы пользователя обновлены" });
        }

        

        [HttpGet("interests/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserInterests(int userId)
        {
            var interests = await _context.UserInterests
                .Where(ui => ui.UserId == userId)
                .Include(ui => ui.Skill)
                .Select(ui => new
                {
                    SkillId = ui.Skill.SkillId,
                    SkillName = ui.Skill.SkillName
                })
                .ToListAsync();

            return Ok(interests);
        }

        [HttpGet("weekly-challenge/{userId}")]
        public async Task<ActionResult<object>> GetWeeklyChallenge(int userId)
        {
            var currentDate = DateTime.UtcNow;
            var calendar = CultureInfo.InvariantCulture.Calendar;
            var weekNumber = calendar.GetWeekOfYear(currentDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var year = currentDate.Year;

            var challenge = await _context.WeeklyChallenges
                .Include(wc => wc.Workout)
                .FirstOrDefaultAsync(wc => wc.UserId == userId && wc.WeekNumber == weekNumber && wc.Year == year);

            if (challenge != null)
            {
                return Ok(new
                {
                    WorkoutId = challenge.Workout.WorkoutId,
                    Title = challenge.Workout.Title,
                    ImageUrl = challenge.Workout.ImageUrl
                });
            }

            var challengeWorkouts = await _context.WorkoutRegistrations
                .Where(wr => wr.UserId == userId)
                .Include(wr => wr.Workout)
                .Select(wr => wr.Workout)
                .ToListAsync();

            if (!challengeWorkouts.Any())
            {
                challengeWorkouts = await _context.Workouts
                    .ToListAsync();
            }

            if (!challengeWorkouts.Any())
            {
                return NotFound(new { message = "Тренировки не найдены" });
            }

            var randomWorkout = challengeWorkouts[new Random().Next(challengeWorkouts.Count)];

            var newChallenge = new WeeklyChallenge
            {
                UserId = userId,
                WorkoutId = randomWorkout.WorkoutId,
                WeekNumber = weekNumber,
                Year = year
            };

            _context.WeeklyChallenges.Add(newChallenge);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                WorkoutId = randomWorkout.WorkoutId,
                Title = randomWorkout.Title,
                ImageUrl = randomWorkout.ImageUrl
            });
        }

        [HttpGet("statistics/{userId}")]
        public async Task<ActionResult<object>> GetUserStatistics(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            // 1. Количество посещений тренировок
            var attendanceCount = await _context.WorkoutAttendance
                .Where(a => a.UserId == userId)
                .CountAsync();

            // 2. Количество уникальных тренировок
            var uniqueWorkouts = await _context.WorkoutAttendance
                .Where(a => a.UserId == userId)
                .Select(a => a.WorkoutId)
                .Distinct()
                .CountAsync();

            // 3. Количество выбранных интересов
            var interestCount = await _context.UserInterests
                .Where(ui => ui.UserId == userId)
                .CountAsync();

            // 4. Последняя посещённая тренировка
            var lastWorkout = await _context.WorkoutAttendance
                .Where(a => a.UserId == userId)
                .Include(a => a.Workout)
                .OrderByDescending(a => a.AttendanceDate)
                .Select(a => new
                {
                    WorkoutId = a.Workout.WorkoutId,
                    Title = a.Workout.Title,
                    AttendanceDate = a.AttendanceDate
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                UserId = userId,
                FullName = user.FullName,
                TotalAttendances = attendanceCount,
                UniqueWorkouts = uniqueWorkouts,
                InterestCount = interestCount,
                LastWorkout = lastWorkout != null ? new
                {
                    lastWorkout.WorkoutId,
                    lastWorkout.Title,
                    AttendanceDate = lastWorkout.AttendanceDate.ToString("yyyy-MM-dd HH:mm")
                } : null
            });
        }

        private async Task<UserSession> CreateUserSession(int userId)
        {
            var token = Guid.NewGuid().ToString();
            var session = new UserSession
            {
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                IsActive = true
            };
            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public class UserUpdateRequest
        {
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Password { get; set; }
            public IFormFile? AvatarFile { get; set; }
        }

        public class UserRegisterRequest
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Password { get; set; }
            public string? Avatar { get; set; }
        }

        public class UserLoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}