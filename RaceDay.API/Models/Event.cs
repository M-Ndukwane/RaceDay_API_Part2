namespace RaceDay.API.Models;

public class Event
{
    public int EventsID { get; set; }

    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public DateTime RegistrationDeadline { get; set; }

    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; }

    public int UserID { get; set; }
    public User Organiser { get; set; } = null!;

    public int VenuesID { get; set; }
    public Venue Venue { get; set; } = null!;

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public ICollection<Enrollment> Enrollments { get; set; }
        = new List<Enrollment>();
}
