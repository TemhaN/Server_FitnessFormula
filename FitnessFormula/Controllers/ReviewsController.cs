using FitnessFormula.Data;
using FitnessFormula.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static FitnessFormula.Controllers.TrainersController;

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
                .Where(r => r.IsApproved)
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
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetTrainerReviews(int trainerId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.TrainerId == trainerId && r.IsApproved)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    TrainerId = r.TrainerId,
                    UserId = r.UserId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate,
                    IsApproved = r.IsApproved,
                    User = new UserDto
                    {
                        UserId = r.UserId,
                        FullName = r.User.FullName,
                        Avatar = r.User.Avatar
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetReviewsByUser(int userId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.UserId == userId && r.IsApproved)
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
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var trainer = await _context.Trainers.FindAsync(request.TrainerId);
            if (trainer == null)
                return NotFound(new { message = "Тренер не найден" });

            // Проверка, посещал ли пользователь тренировку тренера
            var attendance = await _context.WorkoutAttendance
                .Include(wa => wa.Workout)
                .FirstOrDefaultAsync(wa => wa.UserId == request.UserId && wa.Workout.TrainerId == request.TrainerId);
            if (attendance == null)
                return BadRequest(new { message = "Вы не посещали тренировки этого тренера, отзыв оставить нельзя." });

            var review = new Review
            {
                TrainerId = request.TrainerId,
                UserId = request.UserId,
                Rating = request.Rating,
                Comment = request.Comment,
                ReviewDate = DateTime.UtcNow,
                IsApproved = false
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviews), new { id = review.ReviewId }, new
            {
                message = "Отзыв отправлен на модерацию",
                review
            });
        }

        [HttpPut("approve/{reviewId}/trainer/{trainerId}")]
        public async Task<IActionResult> ApproveReview(int reviewId, int trainerId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.TrainerId == trainerId);

            if (review == null)
            {
                return NotFound(new { message = "Отзыв не найден или вы не являетесь тренером." });
            }

            review.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Отзыв успешно одобрен" });
        }

        [HttpDelete("reject/{reviewId}/trainer/{trainerId}")]
        public async Task<IActionResult> RejectReview(int reviewId, int trainerId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.TrainerId == trainerId);

            if (review == null)
            {
                return NotFound(new { message = "Отзыв не найден или вы не являетесь тренером." });
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Отзыв отклонён и удалён" });
        }
        [HttpGet("pending/user/{userId}")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetPendingReviewsByUser(int userId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Trainer)
                .ThenInclude(t => t.User)
                .Include(r => r.User)
                .Where(r => r.UserId == userId && !r.IsApproved)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    TrainerId = r.TrainerId,
                    UserId = r.UserId,
                    Comment = r.Comment,
                    Rating = r.Rating,
                    ReviewDate = r.ReviewDate,
                    IsApproved = r.IsApproved,
                    User = new UserDto
                    {
                        UserId = r.UserId,
                        FullName = r.User.FullName,
                        Email = r.User.Email,
                        PhoneNumber = r.User.PhoneNumber,
                        Avatar = r.User.Avatar,
                        RegistrationDate = r.User.RegistrationDate
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("check-comment/{trainerId}/user/{userId}")]
        public async Task<ActionResult<object>> CheckCommentEligibility(int trainerId, int userId)
        {
            var attendance = await _context.WorkoutAttendance
                .Include(wa => wa.Workout)
                .AnyAsync(wa => wa.UserId == userId && wa.Workout.TrainerId == trainerId);

            var hasReview = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.TrainerId == trainerId);

            return Ok(new
            {
                canComment = attendance && !hasReview
            });
        }
        [HttpGet("pending/trainer/{trainerId}")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetPendingTrainerReviewsForTrainer(int trainerId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.TrainerId == trainerId && !r.IsApproved)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    TrainerId = r.TrainerId,
                    UserId = r.UserId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ReviewDate = r.ReviewDate,
                    IsApproved = r.IsApproved,
                    User = new UserDto
                    {
                        UserId = r.UserId,
                        FullName = r.User.FullName,
                        Avatar = r.User.Avatar
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }
        public class ReviewRequest
        {
            public int TrainerId { get; set; }
            public int UserId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
        }

        public class ReviewDto
        {
            public int ReviewId { get; set; }
            public int TrainerId { get; set; }
            public int UserId { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public DateTime ReviewDate { get; set; }
            public bool IsApproved { get; set; }
            public UserDto User { get; set; }
        }
    }
}