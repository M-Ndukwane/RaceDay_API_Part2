using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class CreateEnrollmentViewModel
    {
        [Required]
        public int EventsID { get; set; }

        [Required]
        public int CategoriesID { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }
    }
}
