using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ProfileViewModel
    {
        public int UsersID { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = "";

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
