using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Models
{
    public class WorkoutRegistration
    {
        [Key]
        public int RegistrationId { get; set; }
        public int WorkoutId { get; set; }
        public Workout Workout { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
