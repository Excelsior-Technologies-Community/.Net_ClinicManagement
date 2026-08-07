using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class FeedbackModel
    {
        public long FeedbackId { get; set; }

        [Required]
        public long CustomerId { get; set; }

        [Required]
        public long AppointmentId { get; set; }

        [Required]
        public long DoctorId { get; set; }

        [Required(ErrorMessage = "Please select rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please enter subject.")]
        [StringLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please enter feedback.")]
        [Display(Name = "Feedback")]
        public string FeedbackMessage { get; set; }

        [Display(Name = "Admin Reply")]
        public string? ReplyMessage { get; set; }

        public bool IsApproved { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

      

        public string? CustomerName { get; set; }

        public string? DoctorName { get; set; }

        public string? AppointmentNo { get; set; }

        public string? DepartmentName { get; set; }

        public string? CustomerImage { get; set; }

        public string? DoctorImage { get; set; }

        public string? DoctorSpecialization { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string? AppointmentStatus { get; set; }
    }
}
