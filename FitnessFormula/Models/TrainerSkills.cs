using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessFormula.Models
{
    public class TrainerSkills
    {
        public int TrainerId { get; set; }
        public int SkillId { get; set; }

        // Навигационные свойства
        [ForeignKey("TrainerId")]
        public Trainer Trainer { get; set; }

        [ForeignKey("SkillId")]
        public Skill Skill { get; set; }
    }
}
