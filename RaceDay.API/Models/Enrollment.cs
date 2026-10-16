namespace RaceDay.API.Models;

public class Enrollment
{
    public int EnrollmentsID { get; set; }

    public DateTime EnrolledAt { get; set; }

    public string Status { get; set; } = "Confirmed";

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    public int CategoriesID { get; set; }
    public Category Category { get; set; } = null!;

    public int EventsID { get; set; }
    public Event Event { get; set; } = null!;

    public int UsersID { get; set; }
    public User Participant { get; set; } = null!;

    public Result? Result { get; set; }
}
