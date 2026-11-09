namespace RaceDay.MVC.Models
{
    public class EnrollmentViewModel
    {
        public int EnrollmentsID { get; set; }
        public int EventsID { get; set; }
        public int CategoriesID { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string Status { get; set; } = "";
    }
}
