using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Models
{
    public class DoctorLeaveModel
    {
        public long LeaveId { get; set; }

        [Required(ErrorMessage = "Please select doctor.")]
        [Display(Name = "Doctor")]
        public long DoctorId { get; set; }

        public string? DoctorName { get; set; }

        public string? DepartmentName { get; set; }

        [Required(ErrorMessage = "Leave From Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Leave From Date")]
        public DateTime LeaveFromDate { get; set; }

        [Required(ErrorMessage = "Leave To Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Leave To Date")]
        public DateTime LeaveToDate { get; set; }

        [Required(ErrorMessage = "Leave Reason is required.")]
        [StringLength(500)]
        [Display(Name = "Leave Reason")]
        public string LeaveReason { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedDate { get; set; }

        public string StatusText
        {
            get
            {
                return IsActive ? "Active" : "Inactive";
            }
        }

        public string LeaveDuration
        {
            get
            {
                return $"{LeaveFromDate:dd MMM yyyy} To {LeaveToDate:dd MMM yyyy}";
            }
        }

        public int TotalLeaveDays
        {
            get
            {
                return (LeaveToDate - LeaveFromDate).Days + 1;
            }
        }
        // Doctor Dropdown
        public List<SelectListItem>? DoctorList { get; set; }
    }
}
