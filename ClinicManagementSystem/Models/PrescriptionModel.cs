using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class PrescriptionModel
    {
        //==================================
        // Primary Details
        //==================================

        public long PrescriptionId { get; set; }

        [Required]
        public long AppointmentId { get; set; }

        [Required]
        public long CustomerId { get; set; }

        [Required]
        public long DoctorId { get; set; }

        //==================================
        // Prescription Details
        //==================================

        [Required(ErrorMessage = "Diagnosis is required.")]
        [Display(Name = "Diagnosis")]
        public string? Diagnosis { get; set; }

        [Display(Name = "Symptoms")]
        public string? Symptoms { get; set; }

        [Required(ErrorMessage = "Medicine is required.")]
        [Display(Name = "Medicines")]
        public string? Medicines { get; set; }

        [Required(ErrorMessage = "Dosage is required.")]
        [Display(Name = "Dosage")]
        public string? Dosage { get; set; }

        [Display(Name = "Instructions")]
        public string? Instructions { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Next Visit Date")]
        public DateTime? NextVisitDate { get; set; }

        public string? PrescriptionStatus { get; set; }

        //==================================
        // Audit Fields
        //==================================

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        //==================================
        // Display Properties
        //==================================

        public string? AppointmentNo { get; set; }

        public string? CustomerName { get; set; }

        public string? MobileNo { get; set; }

        public string? Email { get; set; }

        public string? DoctorName { get; set; }

        public string? DoctorImage { get; set; }

        public string? DepartmentName { get; set; }

        public DateTime? AppointmentDate { get; set; }

        public TimeSpan? AppointmentTime { get; set; }

        //==================================
        // PDF / Print
        //==================================

        public string? ClinicName { get; set; }

        public string? ClinicAddress { get; set; }

        public string? ClinicMobile { get; set; }

        public string? ClinicEmail { get; set; }

        //==================================
        // Doctor Information
        //==================================

        public string? Qualification { get; set; }

        public string? Specialization { get; set; }

        public string? Experience { get; set; }
    }
}
