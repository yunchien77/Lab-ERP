using Xunit;
using LabERP.Models.Handlers;
using LabERP.Interface;
using LabERP.Models.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LabERP.Tests.Handlers
{
    public class FinanceHandlerTests
    {
        private readonly Mock<IFinanceRepository> _mockFinanceRepository;
        private readonly Mock<IBankAccountRepository> _mockBankAccountRepository;
        private readonly Mock<ISalaryRepository> _mockSalaryRepository;
        private readonly FinanceHandler _financeHandler;

        public FinanceHandlerTests()
        {
            _mockFinanceRepository = new Mock<IFinanceRepository>();
            _mockBankAccountRepository = new Mock<IBankAccountRepository>();
            _mockSalaryRepository = new Mock<ISalaryRepository>();
            _financeHandler = new FinanceHandler(
                _mockFinanceRepository.Object,
                _mockBankAccountRepository.Object,
                _mockSalaryRepository.Object);
        }

        #region AddIncome Tests
        [Fact]
        public void AddIncome_ValidParameters_AddsIncomeRecord()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal amount = 1000.50m;
            string description = "Test income";
            string category = "Research";
            string professorId = "prof123";

            // Act
            _financeHandler.AddIncome(laboratoryId, amount, description, category, professorId);

            // Assert
            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.LaboratoryId == laboratoryId &&
                record.Type == "Income" &&
                record.Amount == amount &&
                record.Description == description &&
                record.Category == category &&
                record.CreatedBy == professorId)), Times.Once);
        }

        [Fact]
        public void AddIncome_NullLaboratoryId_AddsRecordWithNullLaboratoryId()
        {
            // Arrange
            string laboratoryId = null;
            decimal amount = 500m;
            string description = "Test income";
            string category = "Research";
            string professorId = "prof123";

            // Act
            _financeHandler.AddIncome(laboratoryId, amount, description, category, professorId);

            // Assert
            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.LaboratoryId == null &&
                record.Type == "Income" &&
                record.Amount == amount)), Times.Once);
        }

        [Fact]
        public void AddIncome_ZeroAmount_AddsRecordWithZeroAmount()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal amount = 0m;
            string description = "Zero income";
            string category = "Test";
            string professorId = "prof123";

            // Act
            _financeHandler.AddIncome(laboratoryId, amount, description, category, professorId);

            // Assert
            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.Amount == 0m &&
                record.Type == "Income")), Times.Once);
        }
        #endregion

        #region AddExpense Tests
        [Fact]
        public void AddExpense_ValidParameters_AddsExpenseRecord()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal amount = 750.25m;
            string description = "Test expense";
            string category = "Equipment";
            string professorId = "prof123";

            // Act
            _financeHandler.AddExpense(laboratoryId, amount, description, category, professorId);

            // Assert
            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.LaboratoryId == laboratoryId &&
                record.Type == "Expense" &&
                record.Amount == amount &&
                record.Description == description &&
                record.Category == category &&
                record.CreatedBy == professorId)), Times.Once);
        }

        [Fact]
        public void AddExpense_EmptyDescription_AddsRecordWithEmptyDescription()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal amount = 100m;
            string description = "";
            string category = "Equipment";
            string professorId = "prof123";

            // Act
            _financeHandler.AddExpense(laboratoryId, amount, description, category, professorId);

            // Assert
            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.Description == "" &&
                record.Type == "Expense")), Times.Once);
        }
        #endregion

        #region GetFinanceRecords Tests
        [Fact]
        public void GetFinanceRecords_ValidLaboratoryId_ReturnsRecords()
        {
            // Arrange
            string laboratoryId = "lab123";
            var expectedRecords = new List<FinanceRecord>
            {
                new FinanceRecord { LaboratoryId = laboratoryId, Type = "Income", Amount = 1000m },
                new FinanceRecord { LaboratoryId = laboratoryId, Type = "Expense", Amount = 500m }
            };

            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(expectedRecords);

            // Act
            var result = _financeHandler.GetFinanceRecords(laboratoryId);

            // Assert
            Assert.Equal(expectedRecords, result);
            _mockFinanceRepository.Verify(r => r.GetByLaboratoryId(laboratoryId), Times.Once);
        }

        [Fact]
        public void GetFinanceRecords_NullLaboratoryId_ReturnsRepositoryResult()
        {
            // Arrange
            string laboratoryId = null;
            var expectedRecords = new List<FinanceRecord>();

            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(expectedRecords);

            // Act
            var result = _financeHandler.GetFinanceRecords(laboratoryId);

            // Assert
            Assert.Equal(expectedRecords, result);
            _mockFinanceRepository.Verify(r => r.GetByLaboratoryId(laboratoryId), Times.Once);
        }
        #endregion

        #region GetBalance Tests
        [Fact]
        public void GetBalance_ValidLaboratoryId_ReturnsBalance()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal expectedBalance = 1500.75m;

            _mockFinanceRepository.Setup(r => r.GetBalance(laboratoryId)).Returns(expectedBalance);

            // Act
            var result = _financeHandler.GetBalance(laboratoryId);

            // Assert
            Assert.Equal(expectedBalance, result);
            _mockFinanceRepository.Verify(r => r.GetBalance(laboratoryId), Times.Once);
        }

        [Fact]
        public void GetBalance_NegativeBalance_ReturnsNegativeValue()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal expectedBalance = -500.50m;

            _mockFinanceRepository.Setup(r => r.GetBalance(laboratoryId)).Returns(expectedBalance);

            // Act
            var result = _financeHandler.GetBalance(laboratoryId);

            // Assert
            Assert.Equal(expectedBalance, result);
        }
        #endregion

        #region SetBankAccount Tests
        [Fact]
        public void SetBankAccount_NewAccount_AddsNewBankAccount()
        {
            // Arrange
            string laboratoryId = "lab123";
            string bankName = "Test Bank";
            string accountNumber = "1234567890";
            string accountHolder = "Test Holder";
            string branchCode = "001";

            _mockBankAccountRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns((BankAccount)null);

            // Act
            _financeHandler.SetBankAccount(laboratoryId, bankName, accountNumber, accountHolder, branchCode);

            // Assert
            _mockBankAccountRepository.Verify(r => r.Add(It.Is<BankAccount>(account =>
                account.LaboratoryId == laboratoryId &&
                account.BankName == bankName &&
                account.AccountNumber == accountNumber &&
                account.AccountHolder == accountHolder &&
                account.BranchCode == branchCode)), Times.Once);
            _mockBankAccountRepository.Verify(r => r.Update(It.IsAny<BankAccount>()), Times.Never);
        }

        [Fact]
        public void SetBankAccount_ExistingAccount_UpdatesBankAccount()
        {
            // Arrange
            string laboratoryId = "lab123";
            string bankName = "Updated Bank";
            string accountNumber = "9876543210";
            string accountHolder = "Updated Holder";
            string branchCode = "002";

            var existingAccount = new BankAccount
            {
                LaboratoryId = laboratoryId,
                BankName = "Old Bank",
                AccountNumber = "1111111111",
                AccountHolder = "Old Holder",
                BranchCode = "001"
            };

            _mockBankAccountRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(existingAccount);

            // Act
            _financeHandler.SetBankAccount(laboratoryId, bankName, accountNumber, accountHolder, branchCode);

            // Assert
            Assert.Equal(bankName, existingAccount.BankName);
            Assert.Equal(accountNumber, existingAccount.AccountNumber);
            Assert.Equal(accountHolder, existingAccount.AccountHolder);
            Assert.Equal(branchCode, existingAccount.BranchCode);
            _mockBankAccountRepository.Verify(r => r.Update(existingAccount), Times.Once);
            _mockBankAccountRepository.Verify(r => r.Add(It.IsAny<BankAccount>()), Times.Never);
        }

        [Fact]
        public void SetBankAccount_NullParameters_HandlesNullValues()
        {
            // Arrange
            string laboratoryId = "lab123";
            string bankName = null;
            string accountNumber = null;
            string accountHolder = null;
            string branchCode = null;

            _mockBankAccountRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns((BankAccount)null);

            // Act
            _financeHandler.SetBankAccount(laboratoryId, bankName, accountNumber, accountHolder, branchCode);

            // Assert
            _mockBankAccountRepository.Verify(r => r.Add(It.Is<BankAccount>(account =>
                account.BankName == null &&
                account.AccountNumber == null &&
                account.AccountHolder == null &&
                account.BranchCode == null)), Times.Once);
        }
        #endregion

        #region GetBankAccount Tests
        [Fact]
        public void GetBankAccount_ValidLaboratoryId_ReturnsBankAccount()
        {
            // Arrange
            string laboratoryId = "lab123";
            var expectedAccount = new BankAccount
            {
                LaboratoryId = laboratoryId,
                BankName = "Test Bank",
                AccountNumber = "1234567890"
            };

            _mockBankAccountRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(expectedAccount);

            // Act
            var result = _financeHandler.GetBankAccount(laboratoryId);

            // Assert
            Assert.Equal(expectedAccount, result);
            _mockBankAccountRepository.Verify(r => r.GetByLaboratoryId(laboratoryId), Times.Once);
        }

        [Fact]
        public void GetBankAccount_AccountNotExists_ReturnsNull()
        {
            // Arrange
            string laboratoryId = "lab123";
            _mockBankAccountRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns((BankAccount)null);

            // Act
            var result = _financeHandler.GetBankAccount(laboratoryId);

            // Assert
            Assert.Null(result);
        }
        #endregion

        #region SetSalary Tests
        [Fact]
        public void SetSalary_NewSalary_AddsSalaryAndExpenseRecord()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";
            decimal newAmount = 50000m;
            string professorId = "prof123";

            _mockSalaryRepository.Setup(r => r.GetByUserAndLaboratory(userId, laboratoryId)).Returns((Salary)null);
            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(new List<FinanceRecord>());

            // Act
            _financeHandler.SetSalary(laboratoryId, userId, userName, newAmount, professorId);

            // Assert
            _mockSalaryRepository.Verify(r => r.Add(It.Is<Salary>(salary =>
                salary.LaboratoryId == laboratoryId &&
                salary.UserId == userId &&
                salary.UserName == userName &&
                salary.Amount == newAmount &&
                salary.CreatedBy == professorId)), Times.Once);

            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.Type == "Expense" &&
                record.Category == "Salary" &&
                record.Amount == newAmount &&
                record.Description.Contains(userName))), Times.Once);
        }

        [Fact]
        public void SetSalary_ExistingSalaryIncrease_UpdatesSalaryAndAddsExpense()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";
            decimal oldAmount = 40000m;
            decimal newAmount = 50000m;
            decimal difference = 10000m;
            string professorId = "prof123";

            var existingSalary = new Salary
            {
                LaboratoryId = laboratoryId,
                UserId = userId,
                UserName = userName,
                Amount = oldAmount
            };

            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var existingRecord = new FinanceRecord
            {
                Type = "Expense",
                Category = "Salary",
                Description = $"薪資支出 - {userName} ({currentMonth}) [UserID: {userId}]",
                CreatedAt = DateTime.Now
            };

            _mockSalaryRepository.Setup(r => r.GetByUserAndLaboratory(userId, laboratoryId)).Returns(existingSalary);
            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(new List<FinanceRecord> { existingRecord });

            // Act
            _financeHandler.SetSalary(laboratoryId, userId, userName, newAmount, professorId);

            // Assert
            Assert.Equal(newAmount, existingSalary.Amount);
            _mockSalaryRepository.Verify(r => r.Update(existingSalary), Times.Once);
            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.Type == "Expense" &&
                record.Category == "Salary" &&
                record.Amount == difference &&
                record.Description.Contains("薪資調增"))), Times.Once);
        }

        [Fact]
        public void SetSalary_ExistingSalaryDecrease_UpdatesSalaryAndAddsIncome()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";
            decimal oldAmount = 50000m;
            decimal newAmount = 40000m;
            decimal difference = -10000m;
            string professorId = "prof123";

            var existingSalary = new Salary
            {
                LaboratoryId = laboratoryId,
                UserId = userId,
                UserName = userName,
                Amount = oldAmount
            };

            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var existingRecord = new FinanceRecord
            {
                Type = "Expense",
                Category = "Salary",
                Description = $"薪資支出 - {userName} ({currentMonth}) [UserID: {userId}]",
                CreatedAt = DateTime.Now
            };

            _mockSalaryRepository.Setup(r => r.GetByUserAndLaboratory(userId, laboratoryId)).Returns(existingSalary);
            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(new List<FinanceRecord> { existingRecord });

            // Act
            _financeHandler.SetSalary(laboratoryId, userId, userName, newAmount, professorId);

            // Assert
            Assert.Equal(newAmount, existingSalary.Amount);
            _mockSalaryRepository.Verify(r => r.Update(existingSalary), Times.Once);
            _mockFinanceRepository.Verify(r => r.Add(It.Is<FinanceRecord>(record =>
                record.Type == "Income" &&
                record.Category == "Salary_Adjustment" &&
                record.Amount == Math.Abs(difference) &&
                record.Description.Contains("薪資調減退回"))), Times.Once);
        }

        [Fact]
        public void SetSalary_ZeroOrNegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";
            decimal invalidAmount = 0m;
            string professorId = "prof123";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _financeHandler.SetSalary(laboratoryId, userId, userName, invalidAmount, professorId));
            Assert.Equal("薪水須設定大於 0", exception.Message);
        }

        [Fact]
        public void SetSalary_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";
            decimal invalidAmount = -1000m;
            string professorId = "prof123";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _financeHandler.SetSalary(laboratoryId, userId, userName, invalidAmount, professorId));
            Assert.Equal("薪水須設定大於 0", exception.Message);
        }

        [Fact]
        public void SetSalary_ExistingSalarySameAmount_UpdatesSalaryNoFinanceRecord()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";
            decimal amount = 50000m;
            string professorId = "prof123";

            var existingSalary = new Salary
            {
                LaboratoryId = laboratoryId,
                UserId = userId,
                UserName = userName,
                Amount = amount
            };

            _mockSalaryRepository.Setup(r => r.GetByUserAndLaboratory(userId, laboratoryId)).Returns(existingSalary);

            // Act
            _financeHandler.SetSalary(laboratoryId, userId, userName, amount, professorId);

            // Assert
            _mockSalaryRepository.Verify(r => r.Update(existingSalary), Times.Once);
            // 沒有差額，不應該新增財務記錄
            _mockFinanceRepository.Verify(r => r.Add(It.IsAny<FinanceRecord>()), Times.Never);
        }

        [Fact]
        public void SetSalary_NewSalaryWithExistingMonthlyRecord_DoesNotAddDuplicateRecord()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";
            decimal newAmount = 50000m;
            string professorId = "prof123";

            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var existingRecord = new FinanceRecord
            {
                Type = "Expense",
                Category = "Salary",
                Description = $"薪資支出 - {userName} ({currentMonth}) [UserID: {userId}]",
                CreatedAt = DateTime.Now
            };

            _mockSalaryRepository.Setup(r => r.GetByUserAndLaboratory(userId, laboratoryId)).Returns((Salary)null);
            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(new List<FinanceRecord> { existingRecord });

            // Act
            _financeHandler.SetSalary(laboratoryId, userId, userName, newAmount, professorId);

            // Assert
            _mockSalaryRepository.Verify(r => r.Add(It.IsAny<Salary>()), Times.Once);
            // 本月已有記錄，不應該新增薪資支出記錄
            _mockFinanceRepository.Verify(r => r.Add(It.IsAny<FinanceRecord>()), Times.Never);
        }
        #endregion

        #region GetSalaries Tests
        [Fact]
        public void GetSalaries_ValidLaboratoryId_ReturnsSalaries()
        {
            // Arrange
            string laboratoryId = "lab123";
            var expectedSalaries = new List<Salary>
            {
                new Salary { LaboratoryId = laboratoryId, UserId = "user1", Amount = 50000m },
                new Salary { LaboratoryId = laboratoryId, UserId = "user2", Amount = 45000m }
            };

            _mockSalaryRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(expectedSalaries);

            // Act
            var result = _financeHandler.GetSalaries(laboratoryId);

            // Assert
            Assert.Equal(expectedSalaries, result);
            _mockSalaryRepository.Verify(r => r.GetByLaboratoryId(laboratoryId), Times.Once);
        }

        [Fact]
        public void GetSalaries_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            string laboratoryId = "lab123";
            var expectedSalaries = new List<Salary>();

            _mockSalaryRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(expectedSalaries);

            // Act
            var result = _financeHandler.GetSalaries(laboratoryId);

            // Assert
            Assert.Empty(result);
        }
        #endregion

        #region GetTotalIncome Tests
        [Fact]
        public void GetTotalIncome_ValidLaboratoryId_ReturnsTotalIncome()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal expectedTotal = 15000.75m;

            _mockFinanceRepository.Setup(r => r.GetTotalIncome(laboratoryId)).Returns(expectedTotal);

            // Act
            var result = _financeHandler.GetTotalIncome(laboratoryId);

            // Assert
            Assert.Equal(expectedTotal, result);
            _mockFinanceRepository.Verify(r => r.GetTotalIncome(laboratoryId), Times.Once);
        }

        [Fact]
        public void GetTotalIncome_ZeroIncome_ReturnsZero()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal expectedTotal = 0m;

            _mockFinanceRepository.Setup(r => r.GetTotalIncome(laboratoryId)).Returns(expectedTotal);

            // Act
            var result = _financeHandler.GetTotalIncome(laboratoryId);

            // Assert
            Assert.Equal(expectedTotal, result);
        }
        #endregion

        #region GetTotalExpense Tests
        [Fact]
        public void GetTotalExpense_ValidLaboratoryId_ReturnsTotalExpense()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal expectedTotal = 8500.25m;

            _mockFinanceRepository.Setup(r => r.GetTotalExpense(laboratoryId)).Returns(expectedTotal);

            // Act
            var result = _financeHandler.GetTotalExpense(laboratoryId);

            // Assert
            Assert.Equal(expectedTotal, result);
            _mockFinanceRepository.Verify(r => r.GetTotalExpense(laboratoryId), Times.Once);
        }

        [Fact]
        public void GetTotalExpense_ZeroExpense_ReturnsZero()
        {
            // Arrange
            string laboratoryId = "lab123";
            decimal expectedTotal = 0m;

            _mockFinanceRepository.Setup(r => r.GetTotalExpense(laboratoryId)).Returns(expectedTotal);

            // Act
            var result = _financeHandler.GetTotalExpense(laboratoryId);

            // Assert
            Assert.Equal(expectedTotal, result);
        }
        #endregion

        #region HasMonthlySalaryRecord Tests
        [Fact]
        public void HasMonthlySalaryRecord_RecordExists_ReturnsTrue()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";

            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var existingRecord = new FinanceRecord
            {
                Type = "Expense",
                Category = "Salary",
                Description = $"薪資支出 - {userName} ({currentMonth}) [UserID: {userId}]",
                CreatedAt = DateTime.Now
            };

            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId))
                .Returns(new List<FinanceRecord> { existingRecord });

            // Act
            var result = _financeHandler.HasMonthlySalaryRecord(laboratoryId, userId, userName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasMonthlySalaryRecord_RecordDoesNotExist_ReturnsFalse()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";

            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId))
                .Returns(new List<FinanceRecord>());

            // Act
            var result = _financeHandler.HasMonthlySalaryRecord(laboratoryId, userId, userName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasMonthlySalaryRecord_DifferentMonth_ReturnsFalse()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userId = "user123";
            string userName = "Test User";

            var pastMonth = DateTime.Now.AddMonths(-1);
            var existingRecord = new FinanceRecord
            {
                Type = "Expense",
                Category = "Salary",
                Description = $"薪資支出 - {userName} ({pastMonth:yyyy-MM}) [UserID: {userId}]",
                CreatedAt = pastMonth
            };

            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId))
                .Returns(new List<FinanceRecord> { existingRecord });

            // Act
            var result = _financeHandler.HasMonthlySalaryRecord(laboratoryId, userId, userName);

            // Assert
            Assert.False(result);
        }
        #endregion

        #region GetSalaryAdjustmentHistory Tests
        [Fact]
        public void GetSalaryAdjustmentHistory_ValidUserName_ReturnsHistory()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userName = "Test User";

            var records = new List<FinanceRecord>
            {
                new FinanceRecord
                {
                    Category = "Salary",
                    Description = $"薪資支出 - {userName} (2024-01)",
                    CreatedAt = new DateTime(2024, 1, 15)
                },
                new FinanceRecord
                {
                    Category = "Salary_Adjustment",
                    Description = $"薪資調增 - {userName} (2024-02)",
                    CreatedAt = new DateTime(2024, 2, 15)
                },
                new FinanceRecord
                {
                    Category = "Salary",
                    Description = $"薪資支出 - Other User (2024-01)",
                    CreatedAt = new DateTime(2024, 1, 15)
                }
            };

            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(records);

            // Act
            var result = _financeHandler.GetSalaryAdjustmentHistory(laboratoryId, userName);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Contains($"- {userName}", r.Description));
            Assert.Equal(new DateTime(2024, 2, 15), result[0].CreatedAt); // 最新的在前面
            Assert.Equal(new DateTime(2024, 1, 15), result[1].CreatedAt);
        }

        [Fact]
        public void GetSalaryAdjustmentHistory_NoRecords_ReturnsEmptyList()
        {
            // Arrange
            string laboratoryId = "lab123";
            string userName = "Test User";

            _mockFinanceRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns((List<FinanceRecord>)null);

            // Act
            var result = _financeHandler.GetSalaryAdjustmentHistory(laboratoryId, userName);

            // Assert
            Assert.Empty(result);
        }
        #endregion

        [Fact]
        public void SetSalary_NewSalaryWithNoMonthlyRecord_AddsNewSalaryExpenseRecord()
        {
            // Arrange
            var laboratoryId = "Lab123";
            var userId = "User123";
            var userName = "Test User";
            var newAmount = 5000m;
            var professorId = "Prof123";
            var currentMonth = DateTime.Now.ToString("yyyy-MM");

            _mockSalaryRepository.Setup(repo => repo.GetByUserAndLaboratory(userId, laboratoryId))
                .Returns((Salary)null); // 沒有現有薪資記錄

            _mockFinanceRepository.Setup(repo => repo.GetByLaboratoryId(laboratoryId))
                .Returns(new List<FinanceRecord>()); // 沒有本月薪資記錄

            // Act
            _financeHandler.SetSalary(laboratoryId, userId, userName, newAmount, professorId);

            // Assert
            _mockFinanceRepository.Verify(repo => repo.Add(It.Is<FinanceRecord>(record =>
                record.Amount == newAmount &&
                record.Description.Contains("薪資") &&
                record.Category == "Salary" &&
                record.LaboratoryId == laboratoryId &&
                record.Type == "Expense" &&
                record.CreatedBy == professorId &&
                record.RecordDate.ToString("yyyy-MM") == currentMonth)), Times.Once);
        }
    }
}
