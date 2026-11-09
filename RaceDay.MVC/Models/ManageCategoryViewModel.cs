using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ManageCategoryViewModel
    {
        public int CategoriesID { get; set; }

        [Required]
        public int EventsID { get; set; }

        [Required]
        public string CategoryName { get; set; } = "";

        [Required]
        public decimal EntryFee { get; set; }

        [Required]
        public int MaxParticipants { get; set; }

        public List<EventViewModel> Events { get; set; } = new();
    }
}
