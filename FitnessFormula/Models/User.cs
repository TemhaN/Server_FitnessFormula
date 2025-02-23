using System.Text.Json.Serialization;

namespace FitnessFormula.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string PasswordHash { get; set; } // Теперь передаётся в запросе

        public string? Avatar { get; set; }
        public DateTime RegistrationDate { get; set; }
    }


}
