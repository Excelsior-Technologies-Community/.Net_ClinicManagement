using System;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class LabReportModel
    {
        public long LabReportId { get; set; }

        [Required(ErrorMessage = "Lab Booking is required.")]
        public long LabBookingId { get; set; }

        [Required(ErrorMessage = "Lab Test is required.")]
        public long LabTestId { get; set; }

        public long CustomerId { get; set; }

        [StringLength(100)]
        public string ReportNo { get; set; }

        [Display(Name = "Report Date")]
        [DataType(DataType.Date)]
        public DateTime? ReportDate { get; set; }

        [Display(Name = "Report File")]
        [StringLength(500)]
        public string ReportFile { get; set; }

        [Display(Name = "Report Title")]
        [StringLength(200)]
        public string ReportTitle { get; set; }

        [Display(Name = "Technician Name")]
        [StringLength(200)]
        public string TechnicianName { get; set; }

        [Required(ErrorMessage = "Report Status is required.")]
        [StringLength(50)]
        public string ReportStatus { get; set; }

        [Display(Name = "Report Remark")]
        public string ReportRemark { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }


        public string BookingNo { get; set; }

        public string TestName { get; set; }

        public string TestCategory { get; set; }

        public string CustomerName { get; set; }

        public string PatientName { get; set; }

        public string MobileNo { get; set; }

        public string Email { get; set; }

        public string TestStatus { get; set; }

        public decimal Amount { get; set; }

        public DateTime? BookingDate { get; set; }
    }
}
