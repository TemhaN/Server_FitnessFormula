using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Models
{
    public class UserInterest
    {
        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required]
        public int SkillId { get; set; }

        public Skill Skill { get; set; }
    }
}
