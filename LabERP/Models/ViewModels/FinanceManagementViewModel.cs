using LabERP.Models.Core;

namespace LabERP.Models.ViewModels
{
    public class FinanceManagementViewModel
    {
        public string LaboratoryId { get; set; } = "";
        public string LaboratoryName { get; set; } = "";
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Balance { get; set; }
        public string CurrentUserRole { get; set; }
        public Professor Creator { get; set; }
        public List<FinanceRecord> RecentRecords { get; set; } = new List<FinanceRecord>();
        public BankAccount? BankAccount { get; set; }
        public List<Salary> Salaries { get; set; } = new List<Salary>();

        public FinanceManagementViewModel()
        {
            RecentRecords = new List<FinanceRecord>();
            Salaries = new List<Salary>();
        }
    }
}
