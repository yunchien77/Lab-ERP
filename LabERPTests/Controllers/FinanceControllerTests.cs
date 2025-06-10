using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;
using LabERP.Controllers;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.ViewModels;

namespace LabERP.Tests.Controllers
{
    public class FinanceControllerTests
    {
        private readonly Mock<IFinanceHandler> _mockFinanceHandler;
        private readonly Mock<ILaboratoryRepository> _mockLaboratoryRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly FinanceController _controller;
        private readonly TempDataDictionary _tempData;

        public FinanceControllerTests()
        {
            _mockFinanceHandler = new Mock<IFinanceHandler>();
            _mockLaboratoryRepository = new Mock<ILaboratoryRepository>();
            _mockUserRepository = new Mock<IUserRepository>();

            _controller = new FinanceController(
                _mockFinanceHandler.Object,
                _mockLaboratoryRepository.Object,
                _mockUserRepository.Object
            );

            // Setup TempData
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>();
            _tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
            _controller.TempData = _tempData;

            // Setup RouteData
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData()
            };
        }

        private void SetupUserContext(string userId = "test-professor-id", string role = "Professor")
        {
            var claims = new List<Claim>
            {
                new Claim("UserID", userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        #region Index Tests

        [Fact]
        public void Index_EmptyLabId_RedirectsToDashboard()
        {
            // Act
            var result = _controller.Index(string.Empty);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室ID不能為空", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Index_NullLabId_RedirectsToDashboard()
        {
            // Act
            var result = _controller.Index(null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室ID不能為空", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Index_LaboratoryNotFound_RedirectsToDashboard()
        {
            // Arrange
            string labId = "non-existent-lab";
            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _controller.Index(labId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室不存在", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Index_ValidLabId_ReturnsViewWithViewModel()
        {
            // Arrange
            string labId = "test-lab-id";
            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = new Professor { UserID = "prof-id", Username = "Prof Test" }
            };
            var financeRecords = new List<FinanceRecord>
            {
                new FinanceRecord { Id = 1, Amount = 1000, Description = "Test Income" },
                new FinanceRecord { Id = 2, Amount = -500, Description = "Test Expense" }
            };
            var bankAccount = new BankAccount
            {
                BankName = "Test Bank",
                AccountNumber = "123456789"
            };
            var salaries = new List<Salary>
            {
                new Salary { UserId = "user1", Amount = 30000 }
            };

            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockFinanceHandler.Setup(x => x.GetTotalIncome(labId)).Returns(5000m);
            _mockFinanceHandler.Setup(x => x.GetTotalExpense(labId)).Returns(2000m);
            _mockFinanceHandler.Setup(x => x.GetBalance(labId)).Returns(3000m);
            _mockFinanceHandler.Setup(x => x.GetFinanceRecords(labId)).Returns(financeRecords);
            _mockFinanceHandler.Setup(x => x.GetBankAccount(labId)).Returns(bankAccount);
            _mockFinanceHandler.Setup(x => x.GetSalaries(labId)).Returns(salaries);

            // Act
            var result = _controller.Index(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<FinanceManagementViewModel>(viewResult.Model);
            Assert.Equal(labId, viewModel.LaboratoryId);
            Assert.Equal("Test Lab", viewModel.LaboratoryName);
            Assert.Equal(5000m, viewModel.TotalIncome);
            Assert.Equal(2000m, viewModel.TotalExpense);
            Assert.Equal(3000m, viewModel.Balance);
            Assert.Equal(2, viewModel.RecentRecords.Count);
            Assert.NotNull(viewModel.BankAccount);
            Assert.Single(viewModel.Salaries);
        }

        [Fact]
        public void Index_FinanceHandlerThrowsException_ReturnsViewWithDefaultValues()
        {
            // Arrange
            string labId = "test-lab-id";
            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = new Professor { UserID = "prof-id", Username = "Prof Test" }
            };

            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockFinanceHandler.Setup(x => x.GetTotalIncome(labId)).Throws(new Exception("Test exception"));
            _mockFinanceHandler.Setup(x => x.GetTotalExpense(labId)).Throws(new Exception("Test exception"));
            _mockFinanceHandler.Setup(x => x.GetBalance(labId)).Throws(new Exception("Test exception"));
            _mockFinanceHandler.Setup(x => x.GetFinanceRecords(labId)).Throws(new Exception("Test exception"));
            _mockFinanceHandler.Setup(x => x.GetBankAccount(labId)).Throws(new Exception("Test exception"));
            _mockFinanceHandler.Setup(x => x.GetSalaries(labId)).Throws(new Exception("Test exception"));

            // Act
            var result = _controller.Index(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<FinanceManagementViewModel>(viewResult.Model);
            Assert.Equal(0m, viewModel.TotalIncome);
            Assert.Equal(0m, viewModel.TotalExpense);
            Assert.Equal(0m, viewModel.Balance);
            Assert.Empty(viewModel.RecentRecords);
            Assert.Null(viewModel.BankAccount);
            Assert.Empty(viewModel.Salaries);
        }

        [Fact]
        public void Index_LaboratoryNameIsNull_UsesDefaultName()
        {
            // Arrange
            string labId = "test-lab-id";
            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = null,
                Creator = new Professor { UserID = "prof-id", Username = "Prof Test" }
            };

            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockFinanceHandler.Setup(x => x.GetTotalIncome(labId)).Returns(0m);
            _mockFinanceHandler.Setup(x => x.GetTotalExpense(labId)).Returns(0m);
            _mockFinanceHandler.Setup(x => x.GetBalance(labId)).Returns(0m);
            _mockFinanceHandler.Setup(x => x.GetFinanceRecords(labId)).Returns(new List<FinanceRecord>());
            _mockFinanceHandler.Setup(x => x.GetBankAccount(labId)).Returns((BankAccount)null);
            _mockFinanceHandler.Setup(x => x.GetSalaries(labId)).Returns(new List<Salary>());

            // Act
            var result = _controller.Index(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<FinanceManagementViewModel>(viewResult.Model);
            Assert.Equal("未知實驗室", viewModel.LaboratoryName);
        }

        #endregion

        #region AddIncome GET Tests

        [Fact]
        public void AddIncome_Get_EmptyLabId_RedirectsToDashboard()
        {
            // Act
            var result = _controller.AddIncome(string.Empty);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室ID不能為空", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void AddIncome_Get_ValidLabId_ReturnsViewWithViewModel()
        {
            // Arrange
            string labId = "test-lab-id";
            var laboratory = new Laboratory { LabID = labId, Name = "Test Lab" };
            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);

            // Act
            var result = _controller.AddIncome(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<AddIncomeViewModel>(viewResult.Model);
            Assert.Equal(labId, viewModel.LaboratoryId);
            Assert.Equal("Test Lab", viewModel.LaboratoryName);
        }

        [Fact]
        public void AddIncome_Get_LaboratoryNameIsNull_UsesDefaultName()
        {
            // Arrange
            string labId = "test-lab-id";
            var laboratory = new Laboratory { LabID = labId, Name = null };
            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);

            // Act
            var result = _controller.AddIncome(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<AddIncomeViewModel>(viewResult.Model);
            Assert.Equal("未知實驗室", viewModel.LaboratoryName);
        }

        #endregion

        #region AddIncome POST Tests

        [Fact]
        public void AddIncome_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            var model = new AddIncomeViewModel();
            _controller.ModelState.AddModelError("Amount", "Required");

            // Act
            var result = _controller.AddIncome(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public void AddIncome_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext();
            var model = new AddIncomeViewModel
            {
                LaboratoryId = "test-lab-id",
                Amount = 1000m,
                Description = "Test Income",
                Category = "Grant"
            };

            _mockFinanceHandler.Setup(x => x.AddIncome(
                model.LaboratoryId, model.Amount, model.Description, model.Category, "test-professor-id"));

            // Act
            var result = _controller.AddIncome(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("test-lab-id", redirectResult.RouteValues["LabID"]);
            Assert.Equal("收入記錄新增成功", _tempData["SuccessMessage"]);
        }

        [Fact]
        public void AddIncome_Post_NoProfessorId_ReturnsViewWithError()
        {
            // Arrange - No user context set
            var model = new AddIncomeViewModel
            {
                LaboratoryId = "test-lab-id",
                Amount = 1000m,
                Description = "Test Income",
                Category = "Grant"
            };

            // Act
            var result = _controller.AddIncome(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("無法取得當前用戶資訊", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void AddIncome_Post_FinanceHandlerThrowsException_ReturnsViewWithError()
        {
            // Arrange
            SetupUserContext();
            var model = new AddIncomeViewModel
            {
                LaboratoryId = "test-lab-id",
                Amount = 1000m,
                Description = "Test Income",
                Category = "Grant"
            };

            _mockFinanceHandler.Setup(x => x.AddIncome(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = _controller.AddIncome(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.True(_controller.ModelState.ContainsKey(""));
        }

        #endregion

        #region SalaryManagement Tests

        [Fact]
        public void SalaryManagement_EmptyLabId_RedirectsToDashboard()
        {
            // Act
            var result = _controller.SalaryManagement(string.Empty);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室ID不能為空", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void SalaryManagement_LaboratoryNotFound_RedirectsToDashboard()
        {
            // Arrange
            string labId = "non-existent-lab";
            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _controller.SalaryManagement(labId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("找不到指定的實驗室", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void SalaryManagement_ValidLabId_ReturnsViewWithViewModel()
        {
            // Arrange
            string labId = "test-lab-id";
            var members = new List<User>
            {
                new User { UserID = "student1", Username = "Student 1", Role = "Student" },
                new User { UserID = "student2", Username = "Student 2", Role = "Student" },
                new User { UserID = "prof1", Username = "Professor 1", Role = "Professor" }
            };
            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Members = members
            };
            var salaries = new List<Salary>
            {
                new Salary
                {
                    Id = 1,
                    UserId = "student1",
                    Amount = 30000,
                    PaymentDate = DateTime.Now.AddMonths(1),
                    Status = "已設定"
                }
            };

            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockFinanceHandler.Setup(x => x.GetSalaries(labId)).Returns(salaries);

            // Act
            var result = _controller.SalaryManagement(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<SalaryManagementViewModel>(viewResult.Model);
            Assert.Equal(labId, viewModel.LaboratoryId);
            Assert.Equal("Test Lab", viewModel.LaboratoryName);
            Assert.Equal(3, viewModel.TotalMembers);
            Assert.Equal(1, viewModel.SalariesSet);
            Assert.Equal(30000m, viewModel.TotalMonthlySalary);
            Assert.Equal(2, viewModel.SalaryItems.Count); // Only students
        }

        [Fact]
        public void SalaryManagement_NullMembersAndSalaries_ReturnsViewWithEmptyData()
        {
            // Arrange
            string labId = "test-lab-id";
            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Members = null
            };

            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockFinanceHandler.Setup(x => x.GetSalaries(labId)).Returns((List<Salary>)null);

            // Act
            var result = _controller.SalaryManagement(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<SalaryManagementViewModel>(viewResult.Model);
            Assert.Equal(0, viewModel.TotalMembers);
            Assert.Equal(0, viewModel.SalariesSet);
            Assert.Equal(0m, viewModel.TotalMonthlySalary);
            Assert.Empty(viewModel.SalaryItems);
        }

        [Fact]
        public void SalaryManagement_LaboratoryNameIsNull_UsesDefaultName()
        {
            // Arrange
            string labId = "test-lab-id";
            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = null,
                Members = new List<User>()
            };

            _mockLaboratoryRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockFinanceHandler.Setup(x => x.GetSalaries(labId)).Returns(new List<Salary>());

            // Act
            var result = _controller.SalaryManagement(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<SalaryManagementViewModel>(viewResult.Model);
            Assert.Equal("未知實驗室", viewModel.LaboratoryName);
        }

        #endregion

        #region UpdateSalary Tests

        [Fact]
        public void UpdateSalary_ZeroAmount_RedirectsWithError()
        {
            // Act
            var result = _controller.UpdateSalary("test-lab-id", "user1", "Test User", 0m);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SalaryManagement", redirectResult.ActionName);
            Assert.Equal("test-lab-id", redirectResult.RouteValues["LabID"]);
            Assert.Equal("薪水須設定大於 0", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void UpdateSalary_NegativeAmount_RedirectsWithError()
        {
            // Act
            var result = _controller.UpdateSalary("test-lab-id", "user1", "Test User", -100m);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SalaryManagement", redirectResult.ActionName);
            Assert.Equal("test-lab-id", redirectResult.RouteValues["LabID"]);
            Assert.Equal("薪水須設定大於 0", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void UpdateSalary_ValidAmount_UpdatesSuccessfully()
        {
            // Arrange
            SetupUserContext();
            _mockFinanceHandler.Setup(x => x.SetSalary(
                "test-lab-id", "user1", "Test User", 30000m, "test-professor-id"));

            // Act
            var result = _controller.UpdateSalary("test-lab-id", "user1", "Test User", 30000m);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SalaryManagement", redirectResult.ActionName);
            Assert.Equal("test-lab-id", redirectResult.RouteValues["LabID"]);
            Assert.Equal("已成功更新 Test User 的薪資", _tempData["SuccessMessage"]);
        }

        [Fact]
        public void UpdateSalary_NoProfessorId_RedirectsWithError()
        {
            // Arrange - No user context set

            // Act
            var result = _controller.UpdateSalary("test-lab-id", "user1", "Test User", 30000m);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SalaryManagement", redirectResult.ActionName);
            Assert.Equal("test-lab-id", redirectResult.RouteValues["LabID"]);
            Assert.Equal("無法取得當前用戶資訊", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void UpdateSalary_FinanceHandlerThrowsException_RedirectsWithError()
        {
            // Arrange
            SetupUserContext();
            _mockFinanceHandler.Setup(x => x.SetSalary(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = _controller.UpdateSalary("test-lab-id", "user1", "Test User", 30000m);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SalaryManagement", redirectResult.ActionName);
            Assert.Equal("test-lab-id", redirectResult.RouteValues["LabID"]);
            Assert.Equal("更新薪資失敗: Test exception", _tempData["ErrorMessage"]);
        }

        #endregion

        #region BankAccountSettings GET Tests

        [Fact]
        public void BankAccountSettings_Get_EmptyLabId_RedirectsToDashboard()
        {
            // Act
            var result = _controller.BankAccountSettings(string.Empty);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室ID不能為空", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void BankAccountSettings_Get_NoExistingAccount_ReturnsEmptyViewModel()
        {
            // Arrange
            string labId = "test-lab-id";
            _mockFinanceHandler.Setup(x => x.GetBankAccount(labId)).Returns((BankAccount)null);

            // Act
            var result = _controller.BankAccountSettings(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<BankAccountViewModel>(viewResult.Model);
            Assert.Equal(labId, viewModel.LaboratoryId);
            Assert.Equal(viewModel.BankName, "");
            Assert.Equal(viewModel.AccountNumber, "");
            Assert.Equal(viewModel.AccountHolder, "");
            Assert.Equal(viewModel.BranchCode, "");
        }

        [Fact]
        public void BankAccountSettings_Get_ExistingAccount_ReturnsFilledViewModel()
        {
            // Arrange
            string labId = "test-lab-id";
            var existingAccount = new BankAccount
            {
                BankName = "Test Bank",
                AccountNumber = "123456789",
                AccountHolder = "Test Holder",
                BranchCode = "001"
            };
            _mockFinanceHandler.Setup(x => x.GetBankAccount(labId)).Returns(existingAccount);

            // Act
            var result = _controller.BankAccountSettings(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<BankAccountViewModel>(viewResult.Model);
            Assert.Equal(labId, viewModel.LaboratoryId);
            Assert.Equal("Test Bank", viewModel.BankName);
            Assert.Equal("123456789", viewModel.AccountNumber);
            Assert.Equal("Test Holder", viewModel.AccountHolder);
            Assert.Equal("001", viewModel.BranchCode);
        }

        #endregion

        #region BankAccountSettings POST Tests

        [Fact]
        public void BankAccountSettings_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            var model = new BankAccountViewModel();
            _controller.ModelState.AddModelError("BankName", "Required");

            // Act
            var result = _controller.BankAccountSettings(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public void BankAccountSettings_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange
            var model = new BankAccountViewModel
            {
                LaboratoryId = "test-lab-id",
                BankName = "Test Bank",
                AccountNumber = "123456789",
                AccountHolder = "Test Holder",
                BranchCode = "001"
            };

            _mockFinanceHandler.Setup(x => x.SetBankAccount(
                model.LaboratoryId, model.BankName, model.AccountNumber, model.AccountHolder, model.BranchCode));

            // Act
            var result = _controller.BankAccountSettings(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("test-lab-id", redirectResult.RouteValues["LabID"]);
            Assert.Equal("銀行帳戶設定成功", _tempData["SuccessMessage"]);
        }

        [Fact]
        public void BankAccountSettings_Post_FinanceHandlerThrowsException_ReturnsViewWithError()
        {
            // Arrange
            var model = new BankAccountViewModel
            {
                LaboratoryId = "test-lab-id",
                BankName = "Test Bank",
                AccountNumber = "123456789",
                AccountHolder = "Test Holder",
                BranchCode = "001"
            };

            _mockFinanceHandler.Setup(x => x.SetBankAccount(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = _controller.BankAccountSettings(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.True(_controller.ModelState.ContainsKey(""));
        }

        #endregion

        #region GetCurrentProfessorId Tests (via reflection or indirect testing)

        [Fact]
        public void GetCurrentProfessorId_UserContextExists_ReturnsUserId()
        {
            // This is tested indirectly through other methods that use GetCurrentProfessorId
            // For example, AddIncome_Post_ValidModel_RedirectsToIndex tests this path
            SetupUserContext("test-user-id");
            var model = new AddIncomeViewModel
            {
                LaboratoryId = "test-lab-id",
                Amount = 1000m,
                Description = "Test Income",
                Category = "Grant"
            };

            _mockFinanceHandler.Setup(x => x.AddIncome(
                model.LaboratoryId, model.Amount, model.Description, model.Category, "test-user-id"));

            var result = _controller.AddIncome(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public void GetCurrentProfessorId_NoUserContext_ReturnsNull()
        {
            // This is tested indirectly through AddIncome_Post_NoProfessorId_ReturnsViewWithError
            // which verifies the null case handling
            var model = new AddIncomeViewModel
            {
                LaboratoryId = "test-lab-id",
                Amount = 1000m,
                Description = "Test Income",
                Category = "Grant"
            };

            var result = _controller.AddIncome(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("無法取得當前用戶資訊", _tempData["ErrorMessage"]);
        }

        #endregion
    }
}