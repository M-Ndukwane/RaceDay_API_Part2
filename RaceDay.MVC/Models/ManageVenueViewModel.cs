using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ManageVenueViewModel
    {
        public int VenuesID { get; set; }

        [Required]
        public string VenueName { get; set; } = "";

        [Required]
        public string AddressLine { get; set; } = "";

        [Required]
        public string City { get; set; } = "";

        [Required]
        public string PostalCode { get; set; } = "";

        [Required]
        public string Province { get; set; } = "";
    }
}
