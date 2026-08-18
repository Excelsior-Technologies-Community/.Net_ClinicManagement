using System;
namespace ClinicManagementSystem.Models
{
    public class HealthPackageBookingModel
    {
        public long PackageBookingId { get; set; }

        public long HealthPackageId { get; set; }

        public long CustomerId { get; set; }

        public string BookingNo { get; set; }

        public DateTime? BookingDate { get; set; }

        public string PatientName { get; set; }

        public string MobileNo { get; set; }

        public string Email { get; set; }

        public DateTime? PreferredDate { get; set; }

        public string PreferredTime { get; set; }

        public string HealthConcern { get; set; }

        public decimal Amount { get; set; }

        public string PaymentStatus { get; set; }

        public string BookingStatus { get; set; }

        public string AdminRemark { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string PackageName { get; set; }

        public string CustomerName { get; set; }
    }
}
