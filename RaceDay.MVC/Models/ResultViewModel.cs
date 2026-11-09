
namespace RaceDay.MVC.Models
{
    public class ResultViewModel
    {
        public int ResultsID { get; set; }

        public int? Position { get; set; }

        public int? FinishTimeSeconds { get; set; }

        public string ResultStatus { get; set; } = "";

        public DateTime RecordedAt { get; set; }

        public int EnrollmentsID { get; set; }
        public string EventName { get; set; } = "";
    }
}
