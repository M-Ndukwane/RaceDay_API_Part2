namespace RaceDay.MVC.Models
{
    public class EventViewModel
    {
        public int EventsID { get; set; }
        public string EventName { get; set; } = "";
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Status { get; set; } = "";
    }
}
