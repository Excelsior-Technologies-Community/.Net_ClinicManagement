using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class MedicalCampModel
    {

        public long CampId { get; set; }


        [Required(ErrorMessage = "Camp Title is required.")]
        [Display(Name = "Camp Title")]
        public string CampTitle { get; set; }

        [Required(ErrorMessage = "Camp Description is required.")]
        [Display(Name = "Camp Description")]
        public string CampDescription { get; set; }

        [Required(ErrorMessage = "Camp Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Camp Date")]
        public DateTime? CampDate { get; set; }

        [Required(ErrorMessage = "Start Time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }

        [Required(ErrorMessage = "End Time is required.")]
        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan? EndTime { get; set; }

        [Required(ErrorMessage = "Venue is required.")]
        [Display(Name = "Venue")]
        public string Venue { get; set; }

        public string? Organizer { get; set; }


        [Required(ErrorMessage = "Please select doctor.")]
        public long? DoctorId { get; set; }

        public string? DoctorName { get; set; }

        [Required(ErrorMessage = "Please select department.")]
        public long? DepartmentId { get; set; }

        public string? DepartmentName { get; set; }


        [Required]
        public decimal RegistrationFee { get; set; }

        [Required]
        public int MaxParticipants { get; set; }

        public int AvailableSeats { get; set; }


        [Required(ErrorMessage = "Contact Number is required.")]
        public string ContactNumber { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Benefits { get; set; }

        public string? Instructions { get; set; }

        public string? CampImage { get; set; }

        public IFormFile? CampImageFile { get; set; }

        public string? CampBanner { get; set; }

        public IFormFile? CampBannerFile { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public List<SelectListItem>? DoctorList { get; set; }

        public List<SelectListItem>? DepartmentList { get; set; }
    }
}
