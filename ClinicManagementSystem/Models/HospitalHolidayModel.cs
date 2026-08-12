using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class HospitalHolidayModel
    {
        public long HolidayId { get; set; }


        [Required(ErrorMessage = "Holiday Title is required.")]
        [Display(Name = "Holiday Title")]
        [StringLength(150)]
        public string HolidayTitle { get; set; }

        [Required(ErrorMessage = "Holiday Date is required.")]
        [Display(Name = "Holiday Date")]
        [DataType(DataType.Date)]
        public DateTime HolidayDate { get; set; }

        [Display(Name = "Holiday Description")]
        [StringLength(500)]
        public string? HolidayDescription { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }


        public string? StatusText
        {
            get
            {
                return IsActive ? "Active" : "Inactive";
            }
        }

        public string? HolidayDateText
        {
            get
            {
                return HolidayDate.ToString("dd MMM yyyy");
            }
        }
    }
}
