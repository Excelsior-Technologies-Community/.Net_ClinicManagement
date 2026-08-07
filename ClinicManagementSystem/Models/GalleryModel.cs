using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class GalleryModel
    {
        public long GalleryId { get; set; }

        [Required(ErrorMessage = "Gallery title is required.")]
        [Display(Name = "Gallery Title")]
        [StringLength(150)]
        public string GalleryTitle { get; set; }

        [Required(ErrorMessage = "Gallery category is required.")]
        [Display(Name = "Gallery Category")]
        public string GalleryCategory { get; set; }

        public string? GalleryImage { get; set; }

        [Display(Name = "Upload Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Display order is required.")]
        [Display(Name = "Display Order")]
        [Range(1, 1000)]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedDate { get; set; }


        public int TotalGallery { get; set; }
    }
}
