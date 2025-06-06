using LabERP.Models.Core;

namespace LabERP.Models.ViewModels
{
    public class FinanceManagementViewModel
    {
        public string LaboratoryId { get; set; }
        public string LaboratoryName { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Balance { get; set; }
        public List<FinanceRecord> RecentRecords { get; set; }
        public BankAccount BankAccount { get; set; }
        public List<Salary> Salaries { get; set; }

        public FinanceManagementViewModel()
        {
            RecentRecords = new List<FinanceRecord>();
            Salaries = new List<Salary>();
        }
    }
}
