using LabERP.Interface;
using LabERP.Models.Core;
using System.Globalization;

namespace LabERP.Models.Handlers
{
    public class FinanceHandler : IFinanceHandler
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

        public void SetSalary(string laboratoryId, string userId, string userName, decimal newAmount, string professorId)
        {
            if (newAmount <= 0)
            {
                throw new ArgumentException("薪水須設定大於 0");
            }

            var existingSalary = _salaryRepository.GetByUserAndLaboratory(userId, laboratoryId);
            var currentMonth = DateTime.Now.ToString("yyyy-MM");

            if (existingSalary != null)
            {
                var oldAmount = existingSalary.Amount;
                var difference = newAmount - oldAmount;

                // 更新薪資記錄
                existingSalary.Amount = newAmount;
                existingSalary.UpdatedAt = DateTime.Now;
                _salaryRepository.Update(existingSalary);

                // 處理薪資差額的財務記錄
                if (difference != 0)
                {
                    HandleSalaryDifference(laboratoryId, userId, userName, oldAmount, newAmount, difference, professorId, currentMonth);
                }

                Console.WriteLine($"薪資更新: {userName} 從 {oldAmount} 更新為 {newAmount}, 差額: {difference}");
            }
            else
            {
                // 新增薪資記錄
                var newSalary = new Salary
                {
                    LaboratoryId = laboratoryId,
                    UserId = userId,
                    UserName = userName,
                    Amount = newAmount,
                    CreatedBy = professorId,
                    CreatedAt = DateTime.Now
                };
                _salaryRepository.Add(newSalary);

                // 檢查本月是否已有薪資支出記錄
                var existingRecord = GetMonthlySalaryRecord(laboratoryId, userId, userName, currentMonth);
                if (existingRecord == null)
                {
                    // 新增本月薪資支出記錄
                    AddSalaryExpenseRecord(laboratoryId, userId, userName, newAmount, professorId, currentMonth);
                    Console.WriteLine($"新增薪資記錄: {userName} - NT${newAmount}");
                }
                else
                {
                    Console.WriteLine($"本月薪資記錄已存在: {userName}");
                }
            }
        }

        private void HandleSalaryDifference(string laboratoryId, string userId, string userName,
                                   decimal oldAmount, decimal newAmount, decimal difference,
                                   string professorId, string currentMonth)
        {
            var existingRecord = GetMonthlySalaryRecord(laboratoryId, userId, userName, currentMonth);

            if (existingRecord != null)
            {
                // 如果本月已有薪資記錄，處理差額
                if (difference > 0)
                {
                    // 薪資增加，新增額外支出
                    AddSalaryExpenseRecord(laboratoryId, userId, userName, difference, professorId, currentMonth, "薪資調增");
                }
                else if (difference < 0)
                {
                    // 薪資減少，新增收入記錄（退回差額）
                    var refundRecord = new FinanceRecord
                    {
                        LaboratoryId = laboratoryId,
                        Type = "Income",
                        Amount = Math.Abs(difference),
                        Description = $"薪資調減退回 - {userName} (從 NT${oldAmount} 調減為 NT${newAmount})",
                        Category = "Salary_Adjustment",
                        CreatedBy = professorId,
                        CreatedAt = DateTime.Now
                    };
                    _financeRepository.Add(refundRecord);
                }
            }
            else
            {
                // 本月還沒有薪資記錄，直接以新薪資建立記錄
                AddSalaryExpenseRecord(laboratoryId, userId, userName, newAmount, professorId, currentMonth);
            }
        }

        private FinanceRecord GetMonthlySalaryRecord(string laboratoryId, string userId, string userName, string monthKey)
        {
            var records = _financeRepository.GetByLaboratoryId(laboratoryId);
            if (records == null) return null;

            // 將 monthKey 轉換為 DateTime 進行比較
            if (!DateTime.TryParseExact(monthKey + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out DateTime targetMonth))
            {
                return null;
            }

            return records.FirstOrDefault(r =>
                r.Type == "Expense" &&
                r.Category == "Salary" &&
                r.Description.Contains($"- {userName}") && // 更精確的用戶名匹配
                r.CreatedAt.Year == targetMonth.Year &&
                r.CreatedAt.Month == targetMonth.Month);
        }

        private void AddSalaryExpenseRecord(string laboratoryId, string userId, string userName, decimal amount,
                                   string professorId, string currentMonth, string adjustmentType = null)
        {
            var description = string.IsNullOrEmpty(adjustmentType)
                ? $"薪資支出 - {userName} ({currentMonth}) [UserID: {userId}]"
                : $"{adjustmentType} - {userName} ({currentMonth}) [UserID: {userId}]";

            var record = new FinanceRecord
            {
                LaboratoryId = laboratoryId,
                Type = "Expense",
                Amount = amount,
                Description = description,
                Category = "Salary",
                CreatedBy = professorId,
                CreatedAt = DateTime.Now
            };

            _financeRepository.Add(record);
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

        // 新增方法：獲取用戶本月薪資狀態
        public bool HasMonthlySalaryRecord(string laboratoryId, string userId, string userName)
        {
            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var record = GetMonthlySalaryRecord(laboratoryId, userId, userName, currentMonth);
            return record != null;
        }

        // 新增方法：獲取薪資調整歷史
        public List<FinanceRecord> GetSalaryAdjustmentHistory(string laboratoryId, string userName)
        {
            var records = _financeRepository.GetByLaboratoryId(laboratoryId);
            return records?.Where(r =>
                (r.Category == "Salary" || r.Category == "Salary_Adjustment") &&
                r.Description.Contains($"- {userName}"))
                .OrderByDescending(r => r.CreatedAt)
                .ToList() ?? new List<FinanceRecord>();
        }
    }
}