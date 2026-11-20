using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty; // "Organiser" or "Participant"
    }
}
