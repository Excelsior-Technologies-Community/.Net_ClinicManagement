using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class PaymentModel
    {
        public long PaymentId { get; set; }

        public long AppointmentId { get; set; }

        public long CustomerId { get; set; }

        // Display Fields
        public string? AppointmentNo { get; set; }

        public string? CustomerName { get; set; }

        public string? DoctorName { get; set; }

        public string? DoctorImage { get; set; }

        public string? DepartmentName { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Please select payment method.")]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }

        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }

        public string? PaymentStatus { get; set; }

        [Display(Name = "Payment Date")]
        public DateTime? PaymentDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Appointment Details
        public DateTime? AppointmentDate { get; set; }

        public TimeSpan? AppointmentTime { get; set; }

        public decimal ConsultationFee { get; set; }

        // Optional Fields
        public string? MobileNo { get; set; }

        public string? Email { get; set; }

        public string? Symptoms { get; set; }

        // Receipt
        public string? ReceiptNo { get; set; }

        // Dropdown
        public List<string> PaymentMethodList { get; set; } = new List<string>()
        {
            "Cash",
            "UPI",
            "Card",
            "Net Banking"
        };
    }
}
