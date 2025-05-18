using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Models
{
    public class Workout
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WorkoutId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public string Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public int TrainerId { get; set; }

        public Trainer Trainer { get; set; }

        [Required]
        public int MaxParticipants { get; set; } = 15;

        public int? GymId { get; set; }

        public Gym? Gym { get; set; }

        public ICollection<WorkoutRegistration> WorkoutRegistrations { get; set; }
        public ICollection<WorkoutComment> WorkoutComments { get; set; }
        public ICollection<WorkoutAttendance> WorkoutAttendances { get; set; }
    }


}
