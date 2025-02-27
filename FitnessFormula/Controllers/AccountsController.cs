using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

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