using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class CampRegistrationModel
    {
        public string CustomerName { get; set; }
        public long CampRegistrationId { get; set; }


        [Required(ErrorMessage = "Medical Camp is required.")]
        public long CampId { get; set; }

        public string CampTitle { get; set; }

        public string CampImage { get; set; }

        public DateTime? CampDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public string Venue { get; set; }

        public string DoctorName { get; set; }

        public string DepartmentName { get; set; }

        public decimal RegistrationFee { get; set; }

        public int MaxParticipants { get; set; }

        public int AvailableSeats { get; set; }

        public long CustomerId { get; set; }

        [Required(ErrorMessage = "Participant name is required.")]
        [StringLength(150)]
        [Display(Name = "Participant Name")]
        public string ParticipantName { get; set; }


        [Required(ErrorMessage = "Mobile number is required.")]
        [Phone(ErrorMessage = "Enter a valid mobile number.")]
        [StringLength(20)]
        [Display(Name = "Mobile Number")]
        public string MobileNo { get; set; }


        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; }


        [Required(ErrorMessage = "Age is required.")]
        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120.")]
        public int? Age { get; set; }


        [Required(ErrorMessage = "Please select gender.")]
        [StringLength(20)]
        public string Gender { get; set; }


        [StringLength(500)]
        [Display(Name = "Health Concern")]
        public string HealthConcern { get; set; }

        public string RegistrationNo { get; set; }

        public DateTime RegistrationDate { get; set; }


        [Required]
        public string RegistrationStatus { get; set; }


        [Required]
        public string PaymentStatus { get; set; }


        public decimal Amount { get; set; }


        [StringLength(500)]
        public string AdminRemark { get; set; }


        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }


        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }


        public List<SelectListItem> GenderList { get; set; }

        public List<SelectListItem> RegistrationStatusList { get; set; }

        public List<SelectListItem> PaymentStatusList { get; set; }
        //==================================================
        // Medical Camp Details
        //==================================================

        public string CampBanner { get; set; }

        public string CampDescription { get; set; }

        public string Organizer { get; set; }

        public string Benefits { get; set; }

        public string Instructions { get; set; }

        public string ContactNumber { get; set; }

        public string CampEmail { get; set; }
    }
}
