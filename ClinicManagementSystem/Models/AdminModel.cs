using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class AdminModel
    {
        public long AdminId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter valid email address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Mobile Number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter valid 10 digit mobile number")]
        public string? MobileNo { get; set; }

        public string? ProfileImage { get; set; }

        public IFormFile? ImageFile { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
