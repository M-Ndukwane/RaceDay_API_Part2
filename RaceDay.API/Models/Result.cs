namespace RaceDay.API.Models;

public class Result
{
    public int ResultsID { get; set; }

    public int? Position { get; set; }
    public int? FinishTimeSeconds { get; set; }

    public string ResultStatus { get; set; } = "Finished";
    public DateTime RecordedAt { get; set; }

    public int EnrollmentsID { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
}
