using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string NotificationType { get; set; } // Reminder, Cancellation, etc.

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public int? WorkoutId { get; set; }

        public Workout? Workout { get; set; }
    }
}
