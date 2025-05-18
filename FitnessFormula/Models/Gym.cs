using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Models
{
    public class Gym
    {
        [Key]
        public int GymId { get; set; }

        [Required]
        public string GymName { get; set; }

        [Required]
        public string Address { get; set; }

        public ICollection<Workout> Workouts { get; set; }
    }
}
