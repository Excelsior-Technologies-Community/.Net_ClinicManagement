using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class ContactInquiryModel
    {
        public long InquiryId { get; set; }

        public long? CustomerId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Display(Name = "Mobile Number")]
        [RegularExpression(@"^[0-9]{10}$",
            ErrorMessage = "Enter a valid 10 digit mobile number.")]
        public string MobileNo { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [Display(Name = "Message")]
        public string Message { get; set; }

        [Display(Name = "Reply")]
        public string? ReplyMessage { get; set; }

        public bool IsReplied { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

      
        public string? CustomerName { get; set; }

        public string? CustomerImage { get; set; }

        public string? StatusText
        {
            get
            {
                return IsReplied ? "Replied" : "Pending";
            }
        }
    }
}
