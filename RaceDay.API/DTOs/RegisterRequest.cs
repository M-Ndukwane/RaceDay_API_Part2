using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [Required]
        [MinLength(8)]
        [StringLength(128)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            "^(Participant|Organiser)$",
            ErrorMessage = "Role must be Participant or Organiser.")]
        public string Role { get; set; } = "Participant";
    }
}