using System;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class LabTestModel
    {
        public long LabTestId { get; set; }

        [Required(ErrorMessage = "Test Name is required.")]
        [Display(Name = "Test Name")]
        [StringLength(200)]
        public string TestName { get; set; }

        [Display(Name = "Test Category")]
        [StringLength(150)]
        public string TestCategory { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Sample Type")]
        [StringLength(100)]
        public string SampleType { get; set; }

        [Display(Name = "Preparation Instructions")]
        public string PreparationInstructions { get; set; }

        [Display(Name = "Report Time")]
        [StringLength(100)]
        public string ReportTime { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 999999999, ErrorMessage = "Please enter a valid price.")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
