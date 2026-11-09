namespace RaceDay.MVC.Models
{
    public class ParticipantDashboardViewModel
    {
        public List<EventViewModel> Events { get; set; } = new();

        public List<CategoryViewModel> Categories { get; set; } = new();
    }
}
