using System;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class LabBookingModel
    {
        public long LabBookingId { get; set; }

        [Required(ErrorMessage = "Lab Test is required.")]
        public long LabTestId { get; set; }

        public long CustomerId { get; set; }

        [StringLength(100)]
        public string BookingNo { get; set; }

        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Patient Name is required.")]
        [Display(Name = "Patient Name")]
        [StringLength(200)]
        public string PatientName { get; set; }

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Display(Name = "Mobile Number")]
        [StringLength(50)]
        public string MobileNo { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; }

        [Range(1, 120, ErrorMessage = "Please enter a valid age.")]
        public int? Age { get; set; }

        [StringLength(50)]
        public string Gender { get; set; }

        [Display(Name = "Preferred Date")]
        [DataType(DataType.Date)]
        public DateTime? PreferredDate { get; set; }

        [Display(Name = "Preferred Time")]
        [StringLength(50)]
        public string PreferredTime { get; set; }

        [Display(Name = "Health Concern")]
        public string HealthConcern { get; set; }

        [Required(ErrorMessage = "Test Status is required.")]
        [StringLength(50)]
        public string TestStatus { get; set; }

        [Required(ErrorMessage = "Payment Status is required.")]
        [StringLength(50)]
        public string PaymentStatus { get; set; }

        [Range(0, 999999999, ErrorMessage = "Please enter a valid amount.")]
        public decimal Amount { get; set; }

        [Display(Name = "Admin Remark")]
        public string AdminRemark { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }


        [Display(Name = "Test Name")]
        public string TestName { get; set; }

        [Display(Name = "Test Category")]
        public string TestCategory { get; set; }

        public string SampleType { get; set; }

        public string ReportTime { get; set; }

        public decimal TestPrice { get; set; }

        public string CustomerName { get; set; }
    }
}
