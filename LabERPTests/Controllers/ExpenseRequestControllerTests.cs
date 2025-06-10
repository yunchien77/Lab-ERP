
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
    public class ExpenseRequestControllerTests
    {
        private readonly Mock<IExpenseRequestHandler> _mockExpenseRequestHandler;
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly Mock<IFinanceRepository> _mockFinanceRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly ExpenseRequestController _controller;
        private readonly Mock<ITempDataProvider> _tempDataProvider;
        private readonly TempDataDictionary _tempData;

        public ExpenseRequestControllerTests()
        {
            _mockExpenseRequestHandler = new Mock<IExpenseRequestHandler>(MockBehavior.Strict);
            _mockLabRepository = new Mock<ILaboratoryRepository>();
            _mockFinanceRepository = new Mock<IFinanceRepository>();
            _mockUserRepository = new Mock<IUserRepository>();

            _controller = new ExpenseRequestController(
                _mockExpenseRequestHandler.Object,
                _mockLabRepository.Object,
                _mockFinanceRepository.Object,
                _mockUserRepository.Object);

            // Setup controller context
            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>();
            _tempDataProvider = tempDataProvider;
            _tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
            _controller.TempData = _tempData;
        }

        private void SetupUserContext(string userId = "test-user", string role = "Student")
        {
            var claims = new List<Claim>
            {
                new Claim("UserID", userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private Laboratory CreateTestLaboratory(string labId = "test-lab", string creatorId = "prof-1")
        {
            var professor = new Professor
            {
                UserID = creatorId,
                Username = "Professor Test",
                Role = "Professor"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Laboratory",
                Creator = professor,
                Members = new List<User>
                {
                    new User { UserID = "student-1", Username = "Student 1", Role = "Student" },
                    new User { UserID = "student-2", Username = "Student 2", Role = "Student" }
                }
            };

            return laboratory;
        }

        private ExpenseRequest CreateTestExpenseRequest(int id = 1, string requesterId = "student-1", ExpenseRequestStatus status = ExpenseRequestStatus.Pending)
        {
            return new ExpenseRequest
            {
                Id = id,
                LaboratoryId = "test-lab",
                RequesterId = requesterId,
                RequesterName = "Test Student",
                Amount = 1000,
                InvoiceNumber = "INV-001",
                Category = "材料費",
                Description = "Test description",
                Purpose = "Test purpose",
                Status = status,
                RequestDate = DateTime.Now,
                Attachments = new List<ExpenseAttachment>()
            };
        }

        [Fact]
        public void Index_UserNotLabMember_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("non-member", "Student");
            var laboratory = CreateTestLaboratory();
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);

            // Act
            var result = _controller.Index("test-lab");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("您不是此實驗室的成員", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Index_ProfessorIsLabCreator_ReturnsViewWithAllRequests()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var laboratory = CreateTestLaboratory();
            var expenseRequests = new List<ExpenseRequest>
            {
                CreateTestExpenseRequest(1, "student-1", ExpenseRequestStatus.Pending),
                CreateTestExpenseRequest(2, "student-2", ExpenseRequestStatus.Approved)
            };

            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestsByLaboratory("test-lab"))
                .Returns(expenseRequests);
            _mockFinanceRepository.Setup(x => x.GetBalance("test-lab")).Returns(5000);

            // Act
            var result = _controller.Index("test-lab");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ExpenseRequestListViewModel>(viewResult.Model);
            Assert.Equal("test-lab", model.LaboratoryId);
            Assert.Equal("Test Laboratory", model.LaboratoryName);
            Assert.Equal("prof-1", model.CurrentUserId);
            Assert.Equal("Professor", model.CurrentUserRole);
            Assert.True(model.IsProfessor);
            Assert.True(model.IsLabCreator);
            Assert.Equal(5000, model.AvailableBudget);
            Assert.Equal(2, model.ExpenseRequests.Count);
            Assert.Equal(1, model.PendingCount);
            Assert.Equal(1, model.ApprovedCount);
            Assert.Equal(0, model.RejectedCount);
        }

        [Fact]
        public void Index_StudentIsMember_ReturnsViewWithOwnRequests()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var laboratory = CreateTestLaboratory();
            var expenseRequests = new List<ExpenseRequest>
            {
                CreateTestExpenseRequest(1, "student-1", ExpenseRequestStatus.Pending),
                CreateTestExpenseRequest(2, "student-2", ExpenseRequestStatus.Approved)
            };

            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestsByLaboratory("test-lab"))
                .Returns(expenseRequests);
            _mockFinanceRepository.Setup(x => x.GetBalance("test-lab")).Returns(5000);

            // Act
            var result = _controller.Index("test-lab");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ExpenseRequestListViewModel>(viewResult.Model);
            Assert.Equal("student-1", model.CurrentUserId);
            Assert.Equal("Student", model.CurrentUserRole);
            Assert.False(model.IsProfessor);
            Assert.False(model.IsLabCreator);
            Assert.Single(model.ExpenseRequests); // Only student's own request
            Assert.Equal(1, model.ExpenseRequests.First().Id);
        }

        #region Create GET Tests

        [Fact]
        public void Create_Get_EmptyLabId_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("student-1", "Student");

            // Act
            var result = _controller.Create(string.Empty);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室ID不能為空", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Create_Get_LaboratoryNotFound_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            _mockLabRepository.Setup(x => x.GetById("nonexistent-lab"))
                .Returns((Laboratory)null);

            // Act
            var result = _controller.Create("nonexistent-lab");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("找不到指定的實驗室", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Create_Get_UserNotMember_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("non-member", "Student");
            var laboratory = CreateTestLaboratory();
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);

            // Act
            var result = _controller.Create("test-lab");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("您不是此實驗室的成員", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Create_Get_ValidRequest_ReturnsView()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var laboratory = CreateTestLaboratory();
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);

            // Act
            var result = _controller.Create("test-lab");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateExpenseRequestViewModel>(viewResult.Model);
            Assert.Equal("test-lab", model.LaboratoryId);
            Assert.Equal("Test Laboratory", model.LaboratoryName);
        }

        #endregion

        #region Create POST Tests

        [Fact]
        public void Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var model = new CreateExpenseRequestViewModel();
            _controller.ModelState.AddModelError("Amount", "Required");

            // Act
            var result = _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public void Create_Post_LaboratoryNotFound_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var model = new CreateExpenseRequestViewModel { LaboratoryId = "nonexistent-lab" };
            _mockLabRepository.Setup(x => x.GetById("nonexistent-lab"))
                .Returns((Laboratory)null);

            // Act
            var result = _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("找不到指定的實驗室", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Create_Post_UserNotFound_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var laboratory = CreateTestLaboratory();
            var model = new CreateExpenseRequestViewModel { LaboratoryId = "test-lab" };

            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockUserRepository.Setup(x => x.GetUserById("student-1"))
                .Returns((User)null);

            // Act
            var result = _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("您不是此實驗室的成員", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Create_Post_UserNotLabMember_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("non-member", "Student");
            var laboratory = CreateTestLaboratory();
            var user = new User { UserID = "non-member", Username = "Non Member" };
            var model = new CreateExpenseRequestViewModel { LaboratoryId = "test-lab" };

            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockUserRepository.Setup(x => x.GetUserById("non-member")).Returns(user);

            // Act
            var result = _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("您不是此實驗室的成員", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Create_Post_Success_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var laboratory = CreateTestLaboratory();
            var user = new User { UserID = "student-1", Username = "Student 1" };
            var model = new CreateExpenseRequestViewModel
            {
                LaboratoryId = "test-lab",
                Amount = 1000,
                InvoiceNumber = "INV-001",
                Category = "材料費",
                Description = "Test description",
                Purpose = "Test purpose",
                Attachments = new List<IFormFile>()
            };

            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockUserRepository.Setup(x => x.GetUserById("student-1")).Returns(user);
            _mockExpenseRequestHandler.Setup(x => x.CreateExpenseRequest(
                "test-lab", "student-1", "Student 1", 1000, "INV-001", "材料費",
                "Test description", "Test purpose", It.IsAny<List<IFormFile>>()));

            // Act
            var result = _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("test-lab", redirectResult.RouteValues["LabID"]);
            Assert.Equal("報帳申請已成功提交", _tempData["SuccessMessage"]);
        }

        [Fact]
        public void Create_Post_HandlerThrowsException_ReturnsViewWithError()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var laboratory = CreateTestLaboratory();
            var user = new User { UserID = "student-1", Username = "Student 1" };
            var model = new CreateExpenseRequestViewModel
            {
                LaboratoryId = "test-lab",
                Amount = 1000,
                InvoiceNumber = "INV-001",
                Category = "材料費",
                Description = "Test description",
                Purpose = "Test purpose",
                Attachments = new List<IFormFile>()
            };

            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockUserRepository.Setup(x => x.GetUserById("student-1")).Returns(user);
            _mockExpenseRequestHandler.Setup(x => x.CreateExpenseRequest(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<IFormFile>>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Equal("提交報帳申請時發生錯誤: Test exception", _tempData["ErrorMessage"]);
        }

        #endregion

        #region Details Tests

        [Fact]
        public void Details_ExpenseRequestNotFound_RedirectsToDashboard()
        {
            // Arrange
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns((ExpenseRequest)null);

            // Act
            var result = _controller.Details(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("找不到指定的報帳申請", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Details_LaboratoryNotFound_RedirectsToDashboard()
        {
            // Arrange
            var expenseRequest = CreateTestExpenseRequest();
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab"))
                .Returns((Laboratory)null);

            // Act
            var result = _controller.Details(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("找不到相關的實驗室", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Details_StudentAccessingOthersRequest_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("student-2", "Student");
            var expenseRequest = CreateTestExpenseRequest(1, "student-1");
            var laboratory = CreateTestLaboratory();

            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);

            // Act
            var result = _controller.Details(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("您沒有權限查看此報帳申請", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Details_StudentAccessingOwnRequest_ReturnsView()
        {
            // Arrange
            SetupUserContext("student-1", "Student");
            var expenseRequest = CreateTestExpenseRequest(1, "student-1");
            var laboratory = CreateTestLaboratory();

            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockFinanceRepository.Setup(x => x.GetBalance("test-lab")).Returns(5000);

            // Act
            var result = _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ExpenseRequestDetailViewModel>(viewResult.Model);
            Assert.Equal(expenseRequest, model.ExpenseRequest);
            Assert.Equal("Test Laboratory", model.LaboratoryName);
            Assert.True(model.CanDelete);
            Assert.False(model.CanReview);
            Assert.Equal("student-1", model.CurrentUserId);
            Assert.Equal(5000, model.AvailableBudget);
        }

        [Fact]
        public void Details_ProfessorAccessingRequest_ReturnsView()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var expenseRequest = CreateTestExpenseRequest(1, "student-1");
            var laboratory = CreateTestLaboratory();

            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockFinanceRepository.Setup(x => x.GetBalance("test-lab")).Returns(5000);

            // Act
            var result = _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ExpenseRequestDetailViewModel>(viewResult.Model);
            Assert.Equal(expenseRequest, model.ExpenseRequest);
            Assert.Equal("Test Laboratory", model.LaboratoryName);
            Assert.False(model.CanDelete);
            Assert.True(model.CanReview);
            Assert.Equal("prof-1", model.CurrentUserId);
            Assert.Equal(5000, model.AvailableBudget);
        }

        #endregion

        #region Review GET Tests

        [Fact]
        public void Review_Get_ExpenseRequestNotFound_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns((ExpenseRequest)null);

            // Act
            var result = _controller.Review(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("找不到指定的報帳申請", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Review_Get_AlreadyReviewed_RedirectsToDetails()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var expenseRequest = CreateTestExpenseRequest(1, "student-1", ExpenseRequestStatus.Approved);
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);

            // Act
            var result = _controller.Review(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(1, redirectResult.RouteValues["id"]);
            Assert.Equal("此報帳申請已經審核過了", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Review_Get_LaboratoryNotFound_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var expenseRequest = CreateTestExpenseRequest();
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab"))
                .Returns((Laboratory)null);

            // Act
            var result = _controller.Review(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("找不到相關的實驗室", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Review_Get_NotLabCreator_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("other-prof", "Professor");
            var expenseRequest = CreateTestExpenseRequest();
            var laboratory = CreateTestLaboratory();

            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);

            // Act
            var result = _controller.Review(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("只有實驗室創建者可以審核報帳申請", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Review_Get_ValidRequest_ReturnsView()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var expenseRequest = CreateTestExpenseRequest();
            var laboratory = CreateTestLaboratory();

            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockFinanceRepository.Setup(x => x.GetBalance("test-lab")).Returns(5000);

            // Act
            var result = _controller.Review(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ReviewExpenseRequestViewModel>(viewResult.Model);
            Assert.Equal(1, model.ExpenseRequestId);
            Assert.Equal("test-lab", model.LaboratoryId);
            Assert.Equal("Test Laboratory", model.LaboratoryName);
            Assert.Equal("Test Student", model.RequesterName);
            Assert.Equal(1000, model.Amount);
            Assert.Equal(5000, model.AvailableBudget);
            Assert.False(model.InsufficientBudget);
        }

        [Fact]
        public void Review_Get_InsufficientBudget_SetsInsufficientBudgetFlag()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var expenseRequest = CreateTestExpenseRequest();
            expenseRequest.Amount = 6000; // More than available budget
            var laboratory = CreateTestLaboratory();

            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockLabRepository.Setup(x => x.GetById("test-lab")).Returns(laboratory);
            _mockFinanceRepository.Setup(x => x.GetBalance("test-lab")).Returns(5000);

            // Act
            var result = _controller.Review(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ReviewExpenseRequestViewModel>(viewResult.Model);
            Assert.True(model.InsufficientBudget);
        }

        #endregion

        #region Review POST Tests

        [Fact]
        public void Review_Post_InvalidModel_ReturnsViewWithReloadedData()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var model = new ReviewExpenseRequestViewModel { ExpenseRequestId = 1 };
            var expenseRequest = CreateTestExpenseRequest();

            _controller.ModelState.AddModelError("Approved", "Required");
            _mockExpenseRequestHandler.Setup(x => x.GetExpenseRequestWithAttachments(1))
                .Returns(expenseRequest);
            _mockFinanceRepository.Setup(x => x.GetBalance("test-lab")).Returns(5000);

            // Act
            var result = _controller.Review(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<ReviewExpenseRequestViewModel>(viewResult.Model);
            Assert.Equal(expenseRequest.Attachments, returnedModel.Attachments);
            Assert.Equal(5000, returnedModel.AvailableBudget);
            Assert.False(returnedModel.InsufficientBudget);
        }

        [Fact]
        public void Review_Post_Success_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("prof-1", "Professor");
            var model = new ReviewExpenseRequestViewModel
            {
                ExpenseRequestId = 1,
                LaboratoryId = "test-lab",
                Approved = true,
                ReviewNotes = "Approved"
            };

            _mockExpenseRequestHandler.Setup(x => x.ReviewExpenseRequest(1, "prof-1", true, "Approved"));

            // Act
            var result = _controller.Review(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("test-lab", redirectResult.RouteValues["LabID"]);
            Assert.Equal("報帳審核完成 - 通過", _tempData["SuccessMessage"]);
        }
        #endregion
    }
}
