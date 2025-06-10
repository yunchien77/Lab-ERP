using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IFinanceHandler
    {
        // 財務記錄管理
        void AddIncome(string laboratoryId, decimal amount, string description, string category, string professorId);
        void AddExpense(string laboratoryId, decimal amount, string description, string category, string professorId);
        List<FinanceRecord> GetFinanceRecords(string laboratoryId);
        decimal GetBalance(string laboratoryId);
        decimal GetTotalIncome(string laboratoryId);
        decimal GetTotalExpense(string laboratoryId);

        // 銀行帳戶管理
        void SetBankAccount(string laboratoryId, string bankName, string accountNumber, string accountHolder, string branchCode);
        BankAccount GetBankAccount(string laboratoryId);

        // 薪資管理
        void SetSalary(string laboratoryId, string userId, string userName, decimal newAmount, string professorId);
        List<Salary> GetSalaries(string laboratoryId);
        bool HasMonthlySalaryRecord(string laboratoryId, string userId, string userName);
        List<FinanceRecord> GetSalaryAdjustmentHistory(string laboratoryId, string userName);
    }
}