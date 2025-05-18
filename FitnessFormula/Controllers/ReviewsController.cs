using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessFormula.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly FitnessDbContext _context;

        public ReviewsController(FitnessDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetReviews()
        {
            return await _context.Reviews
                .Include(r => r.Trainer)
                .Include(r => r.User)
                .Select(r => new
                {
                    r.ReviewId,
                    r.TrainerId,
                    r.UserId,
                    User = r.User != null ? new
                    {
                        r.User.FullName,
                        r.User.Email,
                        r.User.PhoneNumber,
                        r.User.Avatar
                    } : null,
                    r.Rating,
                    r.Comment,
                    r.ReviewDate
                })
                .ToListAsync();
        }

        [HttpGet("trainer/{trainerId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetReviewsByTrainer(int trainerId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.TrainerId == trainerId)
                .Include(r => r.User)
                .Select(r => new
                {
                    r.ReviewId,
                    r.TrainerId,
                    r.UserId,
                    User = r.User != null ? new
                    {
                        r.User.FullName,
                        r.User.Email,
                        r.User.PhoneNumber,
                        r.User.Avatar
                    } : null,
                    r.Rating,
                    r.Comment,
                    r.ReviewDate
                })
                .ToListAsync();

            if (!reviews.Any())
            {
                return NotFound($"Отзывы для тренера с ID {trainerId} не найдены.");
            }

            return reviews;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetReviewsByUser(int userId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.Trainer)
                .Select(r => new
                {
                    r.ReviewId,
                    r.TrainerId,
                    Trainer = r.Trainer != null ? new
                    {
                        r.Trainer.User.FullName,
                        r.Trainer.Description,
                        r.Trainer.ExperienceYears
                    } : null,
                    r.UserId,
                    r.Rating,
                    r.Comment,
                    r.ReviewDate
                })
                .ToListAsync();

            if (!reviews.Any())
            {
                return NotFound($"Отзывы от пользователя с ID {userId} не найдены.");
            }

            return reviews;
        }

        [HttpPost]
        public async Task<ActionResult<Review>> CreateReview([FromBody] ReviewRequest request)
        {
            var review = new Review
            {
                TrainerId = request.TrainerId,
                UserId = request.UserId,
                Rating = request.Rating,
                Comment = request.Comment,
                ReviewDate = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviews), new { id = review.ReviewId }, review);
        }

        public class ReviewRequest
        {
            public int TrainerId { get; set; }
            public int UserId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
        }
    }
}
