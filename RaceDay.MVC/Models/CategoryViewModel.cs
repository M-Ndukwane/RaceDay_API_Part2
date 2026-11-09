namespace RaceDay.MVC.Models
{
    public class CategoryViewModel
    {
        public int CategoriesID { get; set; }

        public int EventsID { get; set; }

        public string CategoryName { get; set; } = "";

        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; }
    }
}
