using System.ComponentModel.DataAnnotations;

namespace FitnessFormula.Models
{
    public class UserSession
    {
        [Key]
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

}
