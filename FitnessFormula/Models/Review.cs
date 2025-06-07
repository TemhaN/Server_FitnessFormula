using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int TrainerId { get; set; }
        public Trainer Trainer { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public bool IsApproved { get; set; } = false;
        //public Trainer Trainer { get; set; }
    }
}