using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class AppointmentModel
    {
        public long AppointmentId { get; set; }

        public string? AppointmentNo { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public long CustomerId { get; set; }

        public long? DoctorId { get; set; }

        [Required(ErrorMessage = "Please select department.")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Please select appointment date.")]
        [DataType(DataType.Date)]
        public DateTime? AppointmentDate { get; set; }

        [Required(ErrorMessage = "Please select appointment time.")]
        [DataType(DataType.Time)]
        public TimeSpan? AppointmentTime { get; set; }

        [Required(ErrorMessage = "Please enter symptoms.")]
        public string? Symptoms { get; set; }

        public decimal ConsultationFee { get; set; }

        public string? AppointmentStatus { get; set; }

        public string? PaymentStatus { get; set; }

        public string? PrescriptionFile { get; set; }

        public IFormFile? Prescription { get; set; }

        public string? AdminRemark { get; set; }

        public string? CustomerRemark { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // ===========================
        // Display Properties
        // ===========================

        public string? CustomerName { get; set; }

        public string? DoctorName { get; set; }

        public string? DoctorImage { get; set; }

        public string? DepartmentName { get; set; }
        public string Qualification { get; set; }

        public string Specialization { get; set; }

        

        public string MobileNo { get; set; }
        public TimeSpan? AvailableFrom { get; set; }

        public TimeSpan? AvailableTo { get; set; }
        public string Experience { get; set; }


        public string Email { get; set; }
    }
}
