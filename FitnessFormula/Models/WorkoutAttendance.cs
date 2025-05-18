using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessFormula.Models
{
    public class WorkoutAttendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        public int WorkoutId { get; set; }

        public Workout Workout { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; } = DateTime.UtcNow;
    }
}