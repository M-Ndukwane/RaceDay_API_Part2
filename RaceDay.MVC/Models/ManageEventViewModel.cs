using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ManageEventViewModel
    {
        public int EventsID { get; set; }

        [Required]
        public string EventName { get; set; } = "";

        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public DateTime RegistrationDeadline { get; set; }

        public string Status { get; set; } = "Open";

        [Required]
        public int VenuesID { get; set; }

        public List<VenueViewModel> Venues { get; set; } = new();
    }
}
