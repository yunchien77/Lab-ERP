using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace LabERP.Models.ViewModels
{
    public class SalaryManagementViewModel
    {
        public string LaboratoryId { get; set; } = "";
        public string LaboratoryName { get; set; } = "";
        public int TotalMembers { get; set; }
        public int SalariesSet { get; set; }
        public decimal TotalMonthlySalary { get; set; }
        public List<SalaryItem> SalaryItems { get; set; } = new List<SalaryItem>();

        public SalaryManagementViewModel()
        {
            SalaryItems = new List<SalaryItem>();
        }
    }

    public class SalaryItem
    {
        public int SalaryId { get; set; }
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public decimal CurrentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = "";

        [Required(ErrorMessage = "請輸入薪資金額")]
        [Range(0.01, double.MaxValue, ErrorMessage = "薪水須設定大於 0")]
        public decimal NewAmount { get; set; }
    }
}
