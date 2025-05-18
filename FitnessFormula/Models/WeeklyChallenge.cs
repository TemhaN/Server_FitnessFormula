using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessFormula.Models
{
    [Table("weeklychallenges")]
    public class WeeklyChallenge
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("WorkoutId")]
        public int WorkoutId { get; set; }

        [Column("weeknumber")]
        public int WeekNumber { get; set; }

        [Column("year")]
        public int Year { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("WorkoutId")]
        public Workout Workout { get; set; }
    }
}