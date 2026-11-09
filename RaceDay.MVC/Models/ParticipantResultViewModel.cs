namespace RaceDay.MVC.Models
{
    public class ParticipantResultViewModel
    {
        public int ResultsID { get; set; }
        public int Position { get; set; }
        public int? FinishTimeSeconds { get; set; }
        public string ResultStatus { get; set; } = "";
        public DateTime RecordedAt { get; set; }
        public string EventName { get; set; } = "";
    }
}
