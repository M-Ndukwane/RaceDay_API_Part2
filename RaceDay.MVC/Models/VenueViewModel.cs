namespace RaceDay.MVC.Models
{
    public class VenueViewModel
    {
        public int VenuesID { get; set; }

        public string VenueName { get; set; } = "";

        public string AddressLine { get; set; } = "";

        public string City { get; set; } = "";

        public string PostalCode { get; set; } = "";

        public string Province { get; set; } = "";
    }
}
