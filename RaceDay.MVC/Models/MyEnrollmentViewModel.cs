namespace RaceDay.MVC.Models
{
    public class MyEnrollmentViewModel
    {
        public int EnrollmentsID { get; set; }

        public int UsersID { get; set; }

        public int EventsID { get; set; }

        public int CategoriesID { get; set; }

        public DateTime EnrolledAt { get; set; }

        public string Status { get; set; } = "";

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }

        public string EventName { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public decimal EntryFee { get; set; }
    }
}
