using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FitnessFormula.Models
{
    public class WorkoutComment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public int WorkoutId { get; set; }
        [JsonIgnore]
        public Workout Workout { get; set; }

        [Required]
        public int UserId { get; set; }
        [JsonIgnore]
        public User User { get; set; }

        [Required]
        public string CommentText { get; set; }

        [Required]
        public DateTime CommentDate { get; set; } = DateTime.UtcNow;
    }
}
