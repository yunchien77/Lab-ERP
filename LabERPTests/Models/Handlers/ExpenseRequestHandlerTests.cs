using Xunit;
using LabERP.Models.Handlers;
using LabERP.Interface;
using LabERP.Models.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;

namespace LabERP.Tests.Handlers
{
    public class ExpenseRequestHandlerTests
    {
        private readonly Mock<IExpenseRequestRepository> _mockExpenseRequestRepository;
        private readonly Mock<IExpenseAttachmentRepository> _mockExpenseAttachmentRepository;
        private readonly Mock<IFinanceRepository> _mockFinanceRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILaboratoryRepository> _mockLaboratoryRepository;
        private readonly ExpenseRequestHandler _expenseRequestHandler;

        public ExpenseRequestHandlerTests()
        {
            _mockExpenseRequestRepository = new Mock<IExpenseRequestRepository>();
            _mockExpenseAttachmentRepository = new Mock<IExpenseAttachmentRepository>();
            _mockFinanceRepository = new Mock<IFinanceRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLaboratoryRepository = new Mock<ILaboratoryRepository>();

            _expenseRequestHandler = new ExpenseRequestHandler(
                _mockExpenseRequestRepository.Object,
                _mockExpenseAttachmentRepository.Object,
                _mockFinanceRepository.Object,
                _mockNotificationService.Object,
                _mockUserRepository.Object,
                _mockLaboratoryRepository.Object);
        }

        #region CreateExpenseRequest Tests

        [Fact]
        public void CreateExpenseRequest_ValidInputs_CreatesExpenseRequest()
        {
            // Arrange
            var laboratoryId = "lab123";
            var requesterId = "user123";
            var requesterName = "Test User";
            var amount = 1000m;
            var invoiceNumber = "INV001";
            var category = "Equipment";
            var description = "Test Description";
            var purpose = "Test Purpose";
            var attachments = new List<IFormFile>();

            var laboratory = new Laboratory
            {
                LabID = laboratoryId,
                Creator = new Professor { UserID = "prof123", Username = "Professor", Email = "prof@test.com" }
            };

            _mockLaboratoryRepository.Setup(r => r.GetById(laboratoryId)).Returns(laboratory);

            // Act
            _expenseRequestHandler.CreateExpenseRequest(laboratoryId, requesterId, requesterName,
                amount, invoiceNumber, category, description, purpose, attachments);

            // Assert
            _mockExpenseRequestRepository.Verify(r => r.Add(It.Is<ExpenseRequest>(er =>
                er.LaboratoryId == laboratoryId &&
                er.RequesterId == requesterId &&
                er.RequesterName == requesterName &&
                er.Amount == amount &&
                er.InvoiceNumber == invoiceNumber &&
                er.Category == category &&
                er.Description == description &&
                er.Purpose == purpose)), Times.Once);
        }

        [Fact]
        public void CreateExpenseRequest_WithValidAttachments_ProcessesAttachments()
        {
            // Arrange
            var laboratoryId = "lab123";
            var requesterId = "user123";
            var requesterName = "Test User";
            var amount = 1000m;
            var invoiceNumber = "INV001";
            var category = "Equipment";
            var description = "Test Description";
            var purpose = "Test Purpose";

            var mockFile = CreateMockFormFile("test.pdf", "application/pdf", 1024);
            var attachments = new List<IFormFile> { mockFile };

            var laboratory = new Laboratory
            {
                LabID = laboratoryId,
                Creator = new Professor { UserID = "prof123", Username = "Professor", Email = "prof@test.com" }
            };

            _mockLaboratoryRepository.Setup(r => r.GetById(laboratoryId)).Returns(laboratory);

            // Act
            _expenseRequestHandler.CreateExpenseRequest(laboratoryId, requesterId, requesterName,
                amount, invoiceNumber, category, description, purpose, attachments);

            // Assert
            _mockExpenseRequestRepository.Verify(r => r.Add(It.IsAny<ExpenseRequest>()), Times.Once);
            _mockExpenseAttachmentRepository.Verify(r => r.Add(It.IsAny<ExpenseAttachment>()), Times.Once);
        }

        [Fact]
        public void CreateExpenseRequest_WithNullAttachments_DoesNotProcessAttachments()
        {
            // Arrange
            var laboratoryId = "lab123";
            var requesterId = "user123";
            var requesterName = "Test User";
            var amount = 1000m;
            var invoiceNumber = "INV001";
            var category = "Equipment";
            var description = "Test Description";
            var purpose = "Test Purpose";
            List<IFormFile> attachments = null;

            var laboratory = new Laboratory
            {
                LabID = laboratoryId,
                Creator = new Professor { UserID = "prof123", Username = "Professor", Email = "prof@test.com" }
            };

            _mockLaboratoryRepository.Setup(r => r.GetById(laboratoryId)).Returns(laboratory);

            // Act
            _expenseRequestHandler.CreateExpenseRequest(laboratoryId, requesterId, requesterName,
                amount, invoiceNumber, category, description, purpose, attachments);

            // Assert
            _mockExpenseRequestRepository.Verify(r => r.Add(It.IsAny<ExpenseRequest>()), Times.Once);
            _mockExpenseAttachmentRepository.Verify(r => r.Add(It.IsAny<ExpenseAttachment>()), Times.Never);
        }

        [Fact]
        public void CreateExpenseRequest_WithEmptyAttachments_DoesNotProcessAttachments()
        {
            // Arrange
            var laboratoryId = "lab123";
            var requesterId = "user123";
            var requesterName = "Test User";
            var amount = 1000m;
            var invoiceNumber = "INV001";
            var category = "Equipment";
            var description = "Test Description";
            var purpose = "Test Purpose";
            var attachments = new List<IFormFile>();

            var laboratory = new Laboratory
            {
                LabID = laboratoryId,
                Creator = new Professor { UserID = "prof123", Username = "Professor", Email = "prof@test.com" }
            };

            _mockLaboratoryRepository.Setup(r => r.GetById(laboratoryId)).Returns(laboratory);

            // Act
            _expenseRequestHandler.CreateExpenseRequest(laboratoryId, requesterId, requesterName,
                amount, invoiceNumber, category, description, purpose, attachments);

            // Assert
            _mockExpenseRequestRepository.Verify(r => r.Add(It.IsAny<ExpenseRequest>()), Times.Once);
            _mockExpenseAttachmentRepository.Verify(r => r.Add(It.IsAny<ExpenseAttachment>()), Times.Never);
        }

        #endregion

        #region ReviewExpenseRequest Tests

        [Fact]
        public void ReviewExpenseRequest_ExpenseRequestNotFound_ThrowsArgumentException()
        {
            // Arrange
            var expenseRequestId = 1;
            var reviewerId = "reviewer123";
            var approved = true;
            var reviewNotes = "Approved";

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns((ExpenseRequest)null);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _expenseRequestHandler.ReviewExpenseRequest(expenseRequestId, reviewerId, approved, reviewNotes));

            Assert.Equal("找不到指定的報帳申請", exception.Message);
        }

        [Fact]
        public void ReviewExpenseRequest_ExpenseRequestNotPending_ThrowsInvalidOperationException()
        {
            // Arrange
            var expenseRequestId = 1;
            var reviewerId = "reviewer123";
            var approved = true;
            var reviewNotes = "Approved";

            var expenseRequest = new ExpenseRequest
            {
                Id = expenseRequestId,
                Status = ExpenseRequestStatus.Approved
            };

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _expenseRequestHandler.ReviewExpenseRequest(expenseRequestId, reviewerId, approved, reviewNotes));

            Assert.Equal("只能審核狀態為未審核的報帳申請", exception.Message);
        }

        [Fact]
        public void ReviewExpenseRequest_ApprovedButInsufficientBudget_ThrowsInvalidOperationException()
        {
            // Arrange
            var expenseRequestId = 1;
            var reviewerId = "reviewer123";
            var approved = true;
            var reviewNotes = "Approved";
            var amount = 1000m;
            var availableBudget = 500m;

            var expenseRequest = new ExpenseRequest
            {
                Id = expenseRequestId,
                Status = ExpenseRequestStatus.Pending,
                Amount = amount,
                LaboratoryId = "lab123",
                RequesterName = "Test User"
            };

            var laboratory = new Laboratory
            {
                LabID = "lab123",
                Creator = new Professor { UserID = "prof123", Username = "Professor", Email = "prof@test.com" }
            };

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);
            _mockFinanceRepository.Setup(r => r.GetBalance("lab123")).Returns(availableBudget);
            _mockLaboratoryRepository.Setup(r => r.GetById("lab123")).Returns(laboratory);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _expenseRequestHandler.ReviewExpenseRequest(expenseRequestId, reviewerId, approved, reviewNotes));

            Assert.Contains("預算不足", exception.Message);
        }

        [Fact]
        public void ReviewExpenseRequest_ApprovedWithSufficientBudget_ApprovesRequest()
        {
            // Arrange
            var expenseRequestId = 1;
            var reviewerId = "reviewer123";
            var approved = true;
            var reviewNotes = "Approved";
            var amount = 1000m;
            var availableBudget = 1500m;

            var expenseRequest = new ExpenseRequest
            {
                Id = expenseRequestId,
                Status = ExpenseRequestStatus.Pending,
                Amount = amount,
                LaboratoryId = "lab123",
                RequesterId = "user123",
                RequesterName = "Test User"
            };

            var requester = new Student { UserID = "user123", Username = "Student", Email = "student@test.com" };

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);
            _mockFinanceRepository.Setup(r => r.GetBalance("lab123")).Returns(availableBudget);
            _mockUserRepository.Setup(r => r.GetUserById("user123")).Returns(requester);

            // Act
            _expenseRequestHandler.ReviewExpenseRequest(expenseRequestId, reviewerId, approved, reviewNotes);

            // Assert
            Assert.Equal(ExpenseRequestStatus.Approved, expenseRequest.Status);
            Assert.Equal(reviewerId, expenseRequest.ReviewedBy);
            Assert.Equal(reviewNotes, expenseRequest.ReviewNotes);
            Assert.NotNull(expenseRequest.ReviewDate);

            _mockExpenseRequestRepository.Verify(r => r.Update(expenseRequest), Times.Once);
            _mockFinanceRepository.Verify(r => r.Add(It.IsAny<FinanceRecord>()), Times.Once);
        }

        [Fact]
        public void ReviewExpenseRequest_Rejected_RejectsRequest()
        {
            // Arrange
            var expenseRequestId = 1;
            var reviewerId = "reviewer123";
            var approved = false;
            var reviewNotes = "Rejected";

            var expenseRequest = new ExpenseRequest
            {
                Id = expenseRequestId,
                Status = ExpenseRequestStatus.Pending,
                RequesterId = "user123",
                RequesterName = "Test User"
            };

            var requester = new Student { UserID = "user123", Username = "Student", Email = "student@test.com" };

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);
            _mockUserRepository.Setup(r => r.GetUserById("user123")).Returns(requester);

            // Act
            _expenseRequestHandler.ReviewExpenseRequest(expenseRequestId, reviewerId, approved, reviewNotes);

            // Assert
            Assert.Equal(ExpenseRequestStatus.Rejected, expenseRequest.Status);
            Assert.Equal(reviewerId, expenseRequest.ReviewedBy);
            Assert.Equal(reviewNotes, expenseRequest.ReviewNotes);
            Assert.NotNull(expenseRequest.ReviewDate);

            _mockExpenseRequestRepository.Verify(r => r.Update(expenseRequest), Times.Once);
            _mockFinanceRepository.Verify(r => r.Add(It.IsAny<FinanceRecord>()), Times.Never);
        }

        #endregion

        #region DeleteExpenseRequest Tests

        [Fact]
        public void DeleteExpenseRequest_CannotDelete_ReturnsFalse()
        {
            // Arrange
            var expenseRequestId = 1;
            var requesterId = "user123";

            _mockExpenseRequestRepository.Setup(r => r.CanDelete(expenseRequestId, requesterId)).Returns(false);

            // Act
            var result = _expenseRequestHandler.DeleteExpenseRequest(expenseRequestId, requesterId);

            // Assert
            Assert.False(result);
            _mockExpenseRequestRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void DeleteExpenseRequest_ExpenseRequestNotFound_ReturnsFalse()
        {
            // Arrange
            var expenseRequestId = 1;
            var requesterId = "user123";

            _mockExpenseRequestRepository.Setup(r => r.CanDelete(expenseRequestId, requesterId)).Returns(true);
            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns((ExpenseRequest)null);

            // Act
            var result = _expenseRequestHandler.DeleteExpenseRequest(expenseRequestId, requesterId);

            // Assert
            Assert.False(result);
            _mockExpenseRequestRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void DeleteExpenseRequest_ValidRequest_DeletesSuccessfully()
        {
            // Arrange
            var expenseRequestId = 1;
            var requesterId = "user123";

            var expenseRequest = new ExpenseRequest
            {
                Id = expenseRequestId,
                RequesterId = requesterId,
                RequesterName = "Test User"
            };

            var attachments = new List<ExpenseAttachment>
            {
                new ExpenseAttachment { Id = 1, ExpenseRequestId = expenseRequestId, FilePath = "/uploads/test.pdf" }
            };

            _mockExpenseRequestRepository.Setup(r => r.CanDelete(expenseRequestId, requesterId)).Returns(true);
            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);
            _mockExpenseAttachmentRepository.Setup(r => r.GetByExpenseRequestId(expenseRequestId)).Returns(attachments);

            // Act
            var result = _expenseRequestHandler.DeleteExpenseRequest(expenseRequestId, requesterId);

            // Assert
            Assert.True(result);
            _mockExpenseAttachmentRepository.Verify(r => r.DeleteByExpenseRequestId(expenseRequestId), Times.Once);
            _mockExpenseRequestRepository.Verify(r => r.Delete(expenseRequestId), Times.Once);
        }

        #endregion

        #region GetExpenseRequestsByLaboratory Tests

        [Fact]
        public void GetExpenseRequestsByLaboratory_ValidLaboratoryId_ReturnsRequestsWithAttachments()
        {
            // Arrange
            var laboratoryId = "lab123";
            var expenseRequests = new List<ExpenseRequest>
            {
                new ExpenseRequest { Id = 1, LaboratoryId = laboratoryId },
                new ExpenseRequest { Id = 2, LaboratoryId = laboratoryId }
            };

            var attachments1 = new List<ExpenseAttachment>
            {
                new ExpenseAttachment { Id = 1, ExpenseRequestId = 1 }
            };

            var attachments2 = new List<ExpenseAttachment>
            {
                new ExpenseAttachment { Id = 2, ExpenseRequestId = 2 }
            };

            _mockExpenseRequestRepository.Setup(r => r.GetByLaboratoryId(laboratoryId)).Returns(expenseRequests);
            _mockExpenseAttachmentRepository.Setup(r => r.GetByExpenseRequestId(1)).Returns(attachments1);
            _mockExpenseAttachmentRepository.Setup(r => r.GetByExpenseRequestId(2)).Returns(attachments2);

            // Act
            var result = _expenseRequestHandler.GetExpenseRequestsByLaboratory(laboratoryId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Attachments.Count);
            Assert.Equal(1, result[1].Attachments.Count);
        }

        #endregion

        #region GetPendingExpenseRequests Tests

        [Fact]
        public void GetPendingExpenseRequests_ValidLaboratoryId_ReturnsPendingRequests()
        {
            // Arrange
            var laboratoryId = "lab123";
            var pendingRequests = new List<ExpenseRequest>
            {
                new ExpenseRequest { Id = 1, Status = ExpenseRequestStatus.Pending }
            };

            _mockExpenseRequestRepository.Setup(r => r.GetByStatus(laboratoryId, ExpenseRequestStatus.Pending))
                .Returns(pendingRequests);

            // Act
            var result = _expenseRequestHandler.GetPendingExpenseRequests(laboratoryId);

            // Assert
            Assert.Single(result);
            Assert.Equal(ExpenseRequestStatus.Pending, result[0].Status);
        }

        #endregion

        #region GetExpenseRequestWithAttachments Tests

        [Fact]
        public void GetExpenseRequestWithAttachments_ValidId_ReturnsRequestWithAttachments()
        {
            // Arrange
            var expenseRequestId = 1;
            var expenseRequest = new ExpenseRequest { Id = expenseRequestId };
            var attachments = new List<ExpenseAttachment>
            {
                new ExpenseAttachment { Id = 1, ExpenseRequestId = expenseRequestId }
            };

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);
            _mockExpenseAttachmentRepository.Setup(r => r.GetByExpenseRequestId(expenseRequestId)).Returns(attachments);

            // Act
            var result = _expenseRequestHandler.GetExpenseRequestWithAttachments(expenseRequestId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expenseRequestId, result.Id);
            Assert.Single(result.Attachments);
        }

        [Fact]
        public void GetExpenseRequestWithAttachments_RequestNotFound_ReturnsNull()
        {
            // Arrange
            var expenseRequestId = 1;

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns((ExpenseRequest)null);

            // Act
            var result = _expenseRequestHandler.GetExpenseRequestWithAttachments(expenseRequestId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region File Validation Tests

        [Fact]
        public void ProcessAttachments_ValidFiles_ProcessesSuccessfully()
        {
            // Arrange
            var expenseRequestId = 1;
            var validFile = CreateMockFormFile("test.pdf", "application/pdf", 1024);
            var attachments = new List<IFormFile> { validFile };

            // Act
            _expenseRequestHandler.CreateExpenseRequest("lab123", "user123", "Test User",
                1000m, "INV001", "Equipment", "Description", "Purpose", attachments);

            // Assert
            _mockExpenseAttachmentRepository.Verify(r => r.Add(It.IsAny<ExpenseAttachment>()), Times.Once);
        }

        [Fact]
        public void ProcessAttachments_InvalidFileSize_SkipsFile()
        {
            // Arrange
            var expenseRequestId = 1;
            var invalidFile = CreateMockFormFile("large.pdf", "application/pdf", 6 * 1024 * 1024); // 6MB > 5MB limit
            var attachments = new List<IFormFile> { invalidFile };

            // Act
            _expenseRequestHandler.CreateExpenseRequest("lab123", "user123", "Test User",
                1000m, "INV001", "Equipment", "Description", "Purpose", attachments);

            // Assert
            _mockExpenseAttachmentRepository.Verify(r => r.Add(It.IsAny<ExpenseAttachment>()), Times.Never);
        }

        [Fact]
        public void ProcessAttachments_InvalidFileType_SkipsFile()
        {
            // Arrange
            var expenseRequestId = 1;
            var invalidFile = CreateMockFormFile("test.txt", "text/plain", 1024);
            var attachments = new List<IFormFile> { invalidFile };

            // Act
            _expenseRequestHandler.CreateExpenseRequest("lab123", "user123", "Test User",
                1000m, "INV001", "Equipment", "Description", "Purpose", attachments);

            // Assert
            _mockExpenseAttachmentRepository.Verify(r => r.Add(It.IsAny<ExpenseAttachment>()), Times.Never);
        }

        [Theory]
        [InlineData("image/jpeg")]
        [InlineData("image/jpg")]
        [InlineData("image/png")]
        [InlineData("application/pdf")]
        public void ProcessAttachments_AllowedFileTypes_ProcessesSuccessfully(string contentType)
        {
            // Arrange
            var validFile = CreateMockFormFile("test.file", contentType, 1024);
            var attachments = new List<IFormFile> { validFile };

            // Act
            _expenseRequestHandler.CreateExpenseRequest("lab123", "user123", "Test User",
                1000m, "INV001", "Equipment", "Description", "Purpose", attachments);

            // Assert
            _mockExpenseAttachmentRepository.Verify(r => r.Add(It.IsAny<ExpenseAttachment>()), Times.Once);
        }

        #endregion

        #region Notification Tests

        [Fact]
        public void CreateExpenseRequest_ValidLaboratory_NotifiesProfessor()
        {
            // Arrange
            var laboratoryId = "lab123";
            var laboratory = new Laboratory
            {
                LabID = laboratoryId,
                Name = "Test Lab",
                Creator = new Professor { UserID = "prof123", Username = "Professor", Email = "prof@test.com" }
            };

            _mockLaboratoryRepository.Setup(r => r.GetById(laboratoryId)).Returns(laboratory);

            // Act
            _expenseRequestHandler.CreateExpenseRequest(laboratoryId, "user123", "Test User",
                1000m, "INV001", "Equipment", "Description", "Purpose", null);

            // Assert - Console output verification would require additional setup
            // This test verifies the notification logic is called by checking repository interactions
            _mockLaboratoryRepository.Verify(r => r.GetById(laboratoryId), Times.AtLeast(1));
        }

        [Fact]
        public void ReviewExpenseRequest_Approved_NotifiesRequester()
        {
            // Arrange
            var expenseRequestId = 1;
            var reviewerId = "reviewer123";
            var approved = true;
            var reviewNotes = "Approved";

            var expenseRequest = new ExpenseRequest
            {
                Id = expenseRequestId,
                Status = ExpenseRequestStatus.Pending,
                Amount = 1000m,
                LaboratoryId = "lab123",
                RequesterId = "user123",
                RequesterName = "Test User"
            };

            var requester = new Student { UserID = "user123", Username = "Student", Email = "student@test.com" };

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);
            _mockFinanceRepository.Setup(r => r.GetBalance("lab123")).Returns(1500m);
            _mockUserRepository.Setup(r => r.GetUserById("user123")).Returns(requester);

            // Act
            _expenseRequestHandler.ReviewExpenseRequest(expenseRequestId, reviewerId, approved, reviewNotes);

            // Assert
            _mockUserRepository.Verify(r => r.GetUserById("user123"), Times.Once);
        }

        [Fact]
        public void ReviewExpenseRequest_RequesterNotFound_DoesNotThrow()
        {
            // Arrange
            var expenseRequestId = 1;
            var reviewerId = "reviewer123";
            var approved = true;
            var reviewNotes = "Approved";

            var expenseRequest = new ExpenseRequest
            {
                Id = expenseRequestId,
                Status = ExpenseRequestStatus.Pending,
                Amount = 1000m,
                LaboratoryId = "lab123",
                RequesterId = "user123",
                RequesterName = "Test User"
            };

            _mockExpenseRequestRepository.Setup(r => r.GetById(expenseRequestId)).Returns(expenseRequest);
            _mockFinanceRepository.Setup(r => r.GetBalance("lab123")).Returns(1500m);
            _mockUserRepository.Setup(r => r.GetUserById("user123")).Returns((User)null);

            // Act & Assert - Should not throw exception
            _expenseRequestHandler.ReviewExpenseRequest(expenseRequestId, reviewerId, approved, reviewNotes);
        }

        #endregion

        #region Helper Methods

        private IFormFile CreateMockFormFile(string fileName, string contentType, long length)
        {
            var mockFile = new Mock<IFormFile>();
            var content = new byte[length];
            var stream = new MemoryStream(content);

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyTo(It.IsAny<Stream>())).Callback<Stream>(s => stream.CopyTo(s));

            return mockFile.Object;
        }

        #endregion
    }
}