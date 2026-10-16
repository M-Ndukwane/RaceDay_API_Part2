namespace RaceDay.API.Models;

public class Venue
{
    public int VenuesID { get; set; }

    public string VenueName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; }
        = new List<Event>();
}
