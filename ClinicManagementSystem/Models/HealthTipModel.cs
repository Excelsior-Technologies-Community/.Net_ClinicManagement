using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class HealthTipModel
    {
        public long HealthTipId { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        [StringLength(100)]
        public string Category { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [Display(Name = "Health Tip Title")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Short Description is required.")]
        [Display(Name = "Short Description")]
        [StringLength(500)]
        public string ShortDescription { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        public string? TipImage { get; set; }

        [Display(Name = "Health Tip Image")]
        public IFormFile? TipImageFile { get; set; }

        [Required(ErrorMessage = "Author Name is required.")]
        [Display(Name = "Author Name")]
        [StringLength(100)]
        public string AuthorName { get; set; }

        [Required(ErrorMessage = "Display Order is required.")]
        [Display(Name = "Display Order")]
        [Range(1, 9999, ErrorMessage = "Display Order must be greater than 0.")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Featured")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedDate { get; set; }


        public string? StatusText
        {
            get
            {
                return IsActive ? "Active" : "Inactive";
            }
        }

        public string? FeaturedText
        {
            get
            {
                return IsFeatured ? "Featured" : "Normal";
            }
        }

        public string? ImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(TipImage))
                {
                    return "/images/no-image.png";
                }

                return "/HealthTips/" + TipImage;
            }
        }
    }
}
