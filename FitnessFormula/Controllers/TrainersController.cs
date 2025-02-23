using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace FitnessFormula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainersController : ControllerBase
    {
        private readonly FitnessDbContext _context;

        public TrainersController(FitnessDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTrainers()
        {
            return await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.TrainerSkills)
                .ThenInclude(ts => ts.Skill)
                .Select(t => new
                {
                    TrainerId = t.TrainerId,
                    UserId = t.UserId,
                    Description = t.Description ?? "Описание отсутствует",
                    ExperienceYears = t.ExperienceYears,
                    Skills = t.TrainerSkills.Select(ts => new
                    {
                        SkillId = ts.Skill.SkillId,
                        SkillName = ts.Skill.SkillName
                    }).ToList(),
                    User = t.User != null ? new UserDto
                    {
                        UserId = t.User.UserId,
                        FullName = t.User.FullName ?? "Неизвестный",
                        Email = t.User.Email ?? "Нет email",
                        PhoneNumber = t.User.PhoneNumber ?? "Нет телефона",
                        Avatar = t.User.Avatar ?? "Нет аватара",
                        RegistrationDate = t.User.RegistrationDate
                    } : null
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTrainerById(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.TrainerSkills)
                .ThenInclude(ts => ts.Skill)
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer == null) return NotFound(new { message = "Тренер не найден" });

            return Ok(new
            {
                TrainerId = trainer.TrainerId,
                Description = trainer.Description,
                ExperienceYears = trainer.ExperienceYears,
                Skills = trainer.TrainerSkills.Select(ts => new
                {
                    SkillId = ts.Skill.SkillId,
                    SkillName = ts.Skill.SkillName
                }).ToList(),
                User = new UserDto
                {
                    UserId = trainer.User.UserId,
                    FullName = trainer.User.FullName,
                    Email = trainer.User.Email,
                    PhoneNumber = trainer.User.PhoneNumber,
                    Avatar = trainer.User.Avatar,
                    RegistrationDate = trainer.User.RegistrationDate
                }
            });
        }


        [HttpPost]
        public async Task<IActionResult> CreateTrainer([FromBody] TrainerRequest request)
        {
            if (request == null || request.User == null)
                return BadRequest(new { message = "Некорректные данные" });

            if (string.IsNullOrWhiteSpace(request.User.Password))
                return BadRequest(new { message = "Пароль не может быть пустым." });

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.User.Email);
                if (existingUser != null)
                {
                    return Conflict(new { message = "Пользователь с таким email уже существует." });
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.User.Password);

                var user = new User
                {
                    FullName = request.User.FullName,
                    Email = request.User.Email,
                    PhoneNumber = request.User.PhoneNumber,
                    PasswordHash = hashedPassword,
                    Avatar = request.User.Avatar,
                    RegistrationDate = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var trainer = new Trainer
                {
                    UserId = user.UserId,
                    Description = request.Description,
                    ExperienceYears = request.ExperienceYears
                };
                _context.Trainers.Add(trainer);
                await _context.SaveChangesAsync();

                List<int> addedSkillIds = new();
                if (request.SkillIds != null && request.SkillIds.Any())
                {
                    var validSkillIds = await _context.Skills
                        .Where(s => request.SkillIds.Contains(s.SkillId))
                        .Select(s => s.SkillId)
                        .ToListAsync();

                    var trainerSkills = validSkillIds.Select(skillId => new TrainerSkills
                    {
                        TrainerId = trainer.TrainerId,
                        SkillId = skillId
                    }).ToList();

                    _context.TrainerSkills.AddRange(trainerSkills);
                    await _context.SaveChangesAsync();
                    addedSkillIds = validSkillIds;
                }

                var session = await CreateUserSession(user.UserId);
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetTrainerById), new { id = trainer.TrainerId }, new
                {
                    message = "Тренер успешно зарегистрирован",
                    trainer = new
                    {
                        trainerId = trainer.TrainerId,
                        fullName = user.FullName,
                        email = user.Email,
                        experienceYears = trainer.ExperienceYears,
                        skills = addedSkillIds
                    },
                    session = new
                    {
                        token = session.Token,
                        expiresAt = session.ExpiresAt
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Ошибка сервера", error = ex.InnerException?.Message ?? ex.Message });
            }
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

        public class TrainerRequest
        {
            public string Description { get; set; }
            public int ExperienceYears { get; set; }
            public List<int> SkillIds { get; set; } = new();
            public UserRequest User { get; set; }
        }

        public class UserRequest
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Password { get; set; }
            public string Avatar { get; set; }
        }

        public class UserDto
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string Avatar { get; set; }
            public DateTime RegistrationDate { get; set; }
        }
    }
}