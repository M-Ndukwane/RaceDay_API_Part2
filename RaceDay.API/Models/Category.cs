namespace RaceDay.API.Models;

public class Category
{
    public int CategoriesID { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }

    public int EventsID { get; set; }
    public Event Event { get; set; } = null!;

    public ICollection<Enrollment> Enrollments { get; set; }
        = new List<Enrollment>();
}
