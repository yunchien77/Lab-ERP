using LabERP.Interface;
using LabERP.Models.Core;

namespace LabERP.Models.Handlers
{
    public class FinanceHandler
    {
        private readonly IFinanceRepository _financeRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly ISalaryRepository _salaryRepository;

        public FinanceHandler(IFinanceRepository financeRepository,
                             IBankAccountRepository bankAccountRepository,
                             ISalaryRepository salaryRepository)
        {
            _financeRepository = financeRepository;
            _bankAccountRepository = bankAccountRepository;
            _salaryRepository = salaryRepository;
        }

        public void AddIncome(string laboratoryId, decimal amount, string description, string category, string professorId)
        {
            var record = new FinanceRecord
            {
                LaboratoryId = laboratoryId,
                Type = "Income",
                Amount = amount,
                Description = description,
                Category = category,
                CreatedBy = professorId
            };

            _financeRepository.Add(record);
        }

        public void AddExpense(string laboratoryId, decimal amount, string description, string category, string professorId)
        {
            var record = new FinanceRecord
            {
                LaboratoryId = laboratoryId,
                Type = "Expense",
                Amount = amount,
                Description = description,
                Category = category,
                CreatedBy = professorId
            };

            _financeRepository.Add(record);
        }

        public List<FinanceRecord> GetFinanceRecords(string laboratoryId)
        {
            return _financeRepository.GetByLaboratoryId(laboratoryId);
        }

        public decimal GetBalance(string laboratoryId)
        {
            return _financeRepository.GetBalance(laboratoryId);
        }

        public void SetBankAccount(string laboratoryId, string bankName, string accountNumber, string accountHolder, string branchCode)
        {
            var existingAccount = _bankAccountRepository.GetByLaboratoryId(laboratoryId);

            if (existingAccount != null)
            {
                existingAccount.BankName = bankName;
                existingAccount.AccountNumber = accountNumber;
                existingAccount.AccountHolder = accountHolder;
                existingAccount.BranchCode = branchCode;
                _bankAccountRepository.Update(existingAccount);
            }
            else
            {
                var newAccount = new BankAccount
                {
                    LaboratoryId = laboratoryId,
                    BankName = bankName,
                    AccountNumber = accountNumber,
                    AccountHolder = accountHolder,
                    BranchCode = branchCode
                };
                _bankAccountRepository.Add(newAccount);
            }
        }

        public BankAccount GetBankAccount(string laboratoryId)
        {
            return _bankAccountRepository.GetByLaboratoryId(laboratoryId);
        }

        public void SetSalary(string laboratoryId, string userId, string userName, decimal amount, string professorId)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("薪水須設定大於 0");
            }

            var existingSalary = _salaryRepository.GetByUserAndLaboratory(userId, laboratoryId);

            if (existingSalary != null)
            {
                existingSalary.Amount = amount;
                _salaryRepository.Update(existingSalary);
            }
            else
            {
                var newSalary = new Salary
                {
                    LaboratoryId = laboratoryId,
                    UserId = userId,
                    UserName = userName,
                    Amount = amount,
                    CreatedBy = professorId
                };
                _salaryRepository.Add(newSalary);
            }

            // 自動記錄薪資支出
            AddExpense(laboratoryId, amount, $"薪資支出 - {userName}", "Salary", professorId);
        }

        public List<Salary> GetSalaries(string laboratoryId)
        {
            return _salaryRepository.GetByLaboratoryId(laboratoryId);
        }

        public decimal GetTotalIncome(string laboratoryId)
        {
            return _financeRepository.GetTotalIncome(laboratoryId);
        }

        public decimal GetTotalExpense(string laboratoryId)
        {
            return _financeRepository.GetTotalExpense(laboratoryId);
        }
    }
}
