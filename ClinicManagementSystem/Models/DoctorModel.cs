using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class DoctorModel
    {
        public long DoctorId { get; set; }

        [Required(ErrorMessage = "Please select department.")]
        public long DepartmentId { get; set; }

        public string? DepartmentName { get; set; }

        [Required(ErrorMessage = "Doctor name is required.")]
        [Display(Name = "Doctor Name")]
        [StringLength(100)]
        public string? DoctorName { get; set; }

        [Required(ErrorMessage = "Qualification is required.")]
        [Display(Name = "Qualification")]
        [StringLength(200)]
        public string? Qualification { get; set; }

        [Required(ErrorMessage = "Specialization is required.")]
        [Display(Name = "Specialization")]
        [StringLength(200)]
        public string? Specialization { get; set; }

        [Required(ErrorMessage = "Experience is required.")]
        [Range(0, 60, ErrorMessage = "Experience must be between 0 and 60 years.")]
        [Display(Name = "Experience (Years)")]
        public int Experience { get; set; }

        [Required(ErrorMessage = "Consultation fee is required.")]
        [Range(0.01, 100000)]
        [Display(Name = "Consultation Fee")]
        public decimal ConsultationFee { get; set; }

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
        [Display(Name = "Mobile Number")]
        public string? MobileNo { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Please select gender.")]
        public string? Gender { get; set; }

        public string? DoctorImage { get; set; }

        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Available From time is required.")]
        [Display(Name = "Available From")]
        public TimeSpan? AvailableFrom { get; set; }

        [Required(ErrorMessage = "Available To time is required.")]
        [Display(Name = "Available To")]
        public TimeSpan? AvailableTo { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public bool IsOnLeave { get; set; }

        public DateTime? LeaveFromDate { get; set; }

        public DateTime? LeaveToDate { get; set; }

        public string? LeaveReason { get; set; }
    }
}
