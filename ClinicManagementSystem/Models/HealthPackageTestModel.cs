using System;
namespace ClinicManagementSystem.Models
{
    public class HealthPackageTestModel
    {
        public long PackageTestId { get; set; }

        public long HealthPackageId { get; set; }

        public long LabTestId { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string TestName { get; set; }


        public string PackageName { get; set; }


        public string TestCategory { get; set; }

        public decimal Price { get; set; }

        public string SampleType { get; set; }

    }
}
