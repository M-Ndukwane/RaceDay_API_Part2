namespace RaceDay.API.Models;

public class User
{
    public int UsersID { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    // Stores a password hash, never a plain-text password.
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public int RolesID { get; set; }
    public Role Role { get; set; } = null!;

    public ICollection<Event> OrganisedEvents { get; set; }
        = new List<Event>();

    public ICollection<Enrollment> Enrollments { get; set; }
        = new List<Enrollment>();
}
