using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class ClinicInformationModel
    {
        public long ClinicId { get; set; }


        [Required(ErrorMessage = "Clinic Name is required.")]
        [Display(Name = "Clinic Name")]
        [StringLength(200)]
        public string ClinicName { get; set; }

        [Required(ErrorMessage = "About Clinic is required.")]
        [Display(Name = "About Clinic")]
        public string AboutClinic { get; set; }

        [Display(Name = "Mission")]
        public string? Mission { get; set; }

        [Display(Name = "Vision")]
        public string? Vision { get; set; }

        [Display(Name = "Why Choose Us")]
        public string? WhyChooseUs { get; set; }


        [Required(ErrorMessage = "Address is required.")]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Display(Name = "Mobile Number")]
        [Phone]
        public string MobileNo { get; set; }

        [Display(Name = "Alternate Mobile Number")]
        [Phone]
        public string? AlternateMobileNo { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Required(ErrorMessage = "Working Hours is required.")]
        [Display(Name = "Working Hours")]
        public string WorkingHours { get; set; }

        [Display(Name = "Emergency Contact")]
        public string? EmergencyContact { get; set; }

        //=========================================
        // Social Media
        //=========================================

        [Display(Name = "Google Map Link")]
        public string? GoogleMapLink { get; set; }

        [Display(Name = "Facebook Link")]
        public string? FacebookLink { get; set; }

        [Display(Name = "Instagram Link")]
        public string? InstagramLink { get; set; }

        [Display(Name = "Twitter Link")]
        public string? TwitterLink { get; set; }

        [Display(Name = "WhatsApp Number")]
        public string? WhatsAppNo { get; set; }

        public string? ClinicLogo { get; set; }

        public IFormFile? ClinicLogoFile { get; set; }

        public string? BannerImage { get; set; }

        public IFormFile? BannerImageFile { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
