using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class BillingModel
    {
        //=====================================
        // Primary Fields
        //=====================================

        public long BillingId { get; set; }

        public string? BillNo { get; set; }

        [Required]
        public long AppointmentId { get; set; }

        [Required]
        public long CustomerId { get; set; }

        [Required]
        public long DoctorId { get; set; }

        //=====================================
        // Charges
        //=====================================

        [Required(ErrorMessage = "Consultation Fee is required.")]
        public decimal ConsultationFee { get; set; }

        public decimal MedicineCharge { get; set; }

        public decimal LabCharge { get; set; }

        public decimal OtherCharge { get; set; }

        public decimal Discount { get; set; }

        public decimal GSTPercentage { get; set; }

        public decimal GSTAmount { get; set; }

        public decimal TotalAmount { get; set; }

        //=====================================
        // Billing Details
        //=====================================

        public string? PaymentStatus { get; set; }

        public string? BillStatus { get; set; }

        [DataType(DataType.Date)]
        public DateTime BillDate { get; set; }

        public string? Remarks { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        //=====================================
        // Display Properties
        //=====================================

        public string? AppointmentNo { get; set; }

        public string? CustomerName { get; set; }

        public string? MobileNo { get; set; }

        public string? Email { get; set; }

        public string? DoctorName { get; set; }

        public string? DoctorImage { get; set; }

        public string? DepartmentName { get; set; }

        public DateTime? AppointmentDate { get; set; }

        public TimeSpan? AppointmentTime { get; set; }

        public string? PaymentMethod { get; set; }

        public string? TransactionId { get; set; }

        public string? PrescriptionStatus { get; set; }
    }
}
