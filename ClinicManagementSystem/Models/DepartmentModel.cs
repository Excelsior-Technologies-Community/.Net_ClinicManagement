using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class DepartmentModel
    {
        public long DepartmentId { get; set; }

        [Required(ErrorMessage = "Department Name is required")]
        [StringLength(100, ErrorMessage = "Department Name cannot exceed 100 characters")]
        public string? DepartmentName { get; set; }

        [Required(ErrorMessage = "Department Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? DepartmentDescription { get; set; }

        public string? DepartmentImage { get; set; }

        [Display(Name = "Department Image")]
        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Display Order is required")]
        [Range(1, 1000, ErrorMessage = "Display Order must be greater than 0")]
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
