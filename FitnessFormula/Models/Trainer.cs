using FitnessFormula.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Trainer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TrainerId { get; set; }
    public string Description { get; set; }
    public int ExperienceYears { get; set; }
    public int UserId { get; set; }

    public User User { get; set; }

    public ICollection<TrainerSkills> TrainerSkills { get; set; } = new List<TrainerSkills>();
}

