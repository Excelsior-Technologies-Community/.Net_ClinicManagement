using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace ClinicManagementSystem.Models
{
    public class HealthPackageModel
    {
        public long HealthPackageId { get; set; }

        [Required]
        public string PackageName { get; set; }

        public string Category { get; set; }

        public string Description { get; set; }

        public decimal OriginalPrice { get; set; }

        public decimal DiscountPrice { get; set; }

        public string PackageImage { get; set; }

        public string Validity { get; set; }

        public string Benefits { get; set; }

        public string PreparationInstructions { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

       
        public IFormFile PackageImageFile { get; set; }
    }
}
