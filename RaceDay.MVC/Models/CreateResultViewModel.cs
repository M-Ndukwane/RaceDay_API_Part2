
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class CreateResultViewModel
    {
        [Required]
        public int EnrollmentsID { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Position must be greater than zero.")]
        public int? Position { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Finish time cannot be negative.")]
        public int? FinishTimeSeconds { get; set; }

        [Required]
        public string ResultStatus { get; set; } = "Finished";
    }
}
