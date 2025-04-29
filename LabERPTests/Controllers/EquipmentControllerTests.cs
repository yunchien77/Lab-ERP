using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;
using LabERP.Controllers;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.Handlers;
using LabERP.Models.ViewModels;

namespace LabERP.Tests.Controllers
{
    public class EquipmentControllerTests
    {
        private readonly Mock<IEquipmentHandler> _mockEquipmentHandler;
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly EquipmentController _controller;
        private readonly Mock<ITempDataProvider> _tempDataProvider;
        private readonly TempDataDictionary _tempData;

        public EquipmentControllerTests()
        {
            _mockEquipmentHandler = new Mock<IEquipmentHandler>(MockBehavior.Strict);
            _mockLabRepository = new Mock<ILaboratoryRepository>();

            _controller = new EquipmentController(_mockEquipmentHandler.Object, _mockLabRepository.Object);

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
        public void Index_LabDoesNotExist_RedirectsToDashboard()
        {
            // Arrange
            string labId = "non-existent-lab";
            _mockLabRepository.Setup(repo => repo.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _controller.Index(labId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("實驗室不存在", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Index_ValidLabId_ReturnsViewWithEquipments()
        {
            // Arrange
            string labId = "valid-lab-id";
            var lab = new Laboratory { LabID = labId, Name = "Test Lab" };
            var equipments = new List<Equipment>
            {
                new Equipment { EquipmentID = "equip1", Name = "Equipment 1", LaboratoryID = labId },
                new Equipment { EquipmentID = "equip2", Name = "Equipment 2", LaboratoryID = labId }
            };

            _mockLabRepository.Setup(repo => repo.GetById(labId)).Returns(lab);
            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentsByLab(labId)).Returns(equipments);

            // Act
            var result = _controller.Index(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Equipment>>(viewResult.Model);
            Assert.Equal(equipments, model);
            Assert.Equal(lab, viewResult.ViewData["Laboratory"]);
            Assert.Equal(labId, viewResult.ViewData["LaboratoryID"]);
        }

        [Fact]
        public void Create_Get_ReturnsViewWithModel()
        {
            // Arrange
            string labId = "test-lab-id";

            // Act
            var result = _controller.Create(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateEquipmentViewModel>(viewResult.Model);
            Assert.Equal(labId, model.LaboratoryID);
            Assert.Equal(DateTime.Now.Date, model.PurchaseDate.Date);
        }

        [Fact]
        public void Create_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("prof-id", "Professor");

            var model = new CreateEquipmentViewModel
            {
                Name = "Test Equipment",
                Description = "Test Description",
                TotalQuantity = 10,
                PurchaseDate = DateTime.Now,
                LaboratoryID = "test-lab-id"
            };

            _mockEquipmentHandler.Setup(handler => handler.CreateEquipment(
                It.IsAny<string>(),
                It.Is<CreateEquipmentDto>(dto =>
                    dto.Name == model.Name &&
                    dto.Description == model.Description &&
                    dto.TotalQuantity == model.TotalQuantity &&
                    dto.PurchaseDate == model.PurchaseDate &&
                    dto.LaboratoryID == model.LaboratoryID
                )
            )).Returns(new Equipment { EquipmentID = "new-equipment-id" });

            // Act
            var result = _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(model.LaboratoryID, redirectResult.RouteValues["labID"]);
        }

        [Fact]
        public void Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var model = new CreateEquipmentViewModel();
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public void Create_Post_ThrowsException_ReturnsViewWithError()
        {
            // Arrange
            SetupUserContext("prof-id", "Professor");

            var model = new CreateEquipmentViewModel
            {
                Name = "Test Equipment",
                Description = "Test Description",
                TotalQuantity = 10,
                PurchaseDate = DateTime.Now,
                LaboratoryID = "test-lab-id"
            };

            string errorMessage = "Error creating equipment";
            _mockEquipmentHandler.Setup(handler => handler.CreateEquipment(
                It.IsAny<string>(),
                It.IsAny<CreateEquipmentDto>()
            )).Throws(new Exception(errorMessage));

            // Act
            var result = _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.True(_controller.ModelState.ContainsKey(""));
            Assert.Equal(errorMessage, _controller.ModelState[""].Errors[0].ErrorMessage);
        }

        [Fact]
        public void Delete_EquipmentExists_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("prof-id", "Professor");
            string equipmentId = "test-equipment-id";
            string labId = "test-lab-id";

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                Name = "Test Equipment",
                LaboratoryID = labId
            };

            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns(equipment);

            _mockEquipmentHandler.Setup(handler => handler.DeleteEquipment(It.IsAny<string>(), equipmentId))
                .Verifiable();

            // Act
            var result = _controller.Delete(equipmentId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(labId, redirectResult.RouteValues["labID"]);
            _mockEquipmentHandler.Verify();
        }

        [Fact]
        public void Delete_EquipmentDoesNotExist_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("prof-id", "Professor");
            string equipmentId = "non-existent-equipment";

            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns((Equipment)null);

            // Act
            var result = _controller.Delete(equipmentId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("設備不存在", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Delete_ThrowsException_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("prof-id", "Professor");
            string equipmentId = "test-equipment-id";
            string labId = "test-lab-id";
            string errorMessage = "Error deleting equipment";

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                Name = "Test Equipment",
                LaboratoryID = labId
            };

            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns(equipment);

            _mockEquipmentHandler.Setup(handler => handler.DeleteEquipment(It.IsAny<string>(), equipmentId))
                .Throws(new Exception(errorMessage));

            // Act
            var result = _controller.Delete(equipmentId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal(errorMessage, _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Borrow_Get_EquipmentDoesNotExist_RedirectsToDashboard()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            string equipmentId = "non-existent-equipment";

            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns((Equipment)null);

            // Act
            var result = _controller.Borrow(equipmentId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirectResult.ActionName);
            Assert.Equal("User", redirectResult.ControllerName);
            Assert.Equal("設備不存在", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Borrow_Get_NoAvailableQuantity_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            string equipmentId = "test-equipment-id";
            string labId = "test-lab-id";

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                Name = "Test Equipment",
                LaboratoryID = labId,
                AvailableQuantity = 0
            };

            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns(equipment);

            // Act
            var result = _controller.Borrow(equipmentId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(labId, redirectResult.RouteValues["labID"]);
            Assert.Equal("該設備當前沒有可用數量", _tempData["ErrorMessage"]);
        }

        [Fact]
        public void Borrow_Get_ValidEquipment_ReturnsViewWithModel()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            string equipmentId = "test-equipment-id";

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                Name = "Test Equipment",
                LaboratoryID = "test-lab-id",
                AvailableQuantity = 5
            };

            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns(equipment);

            // Act
            var result = _controller.Borrow(equipmentId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<BorrowEquipmentViewModel>(viewResult.Model);
            Assert.Equal(equipmentId, model.EquipmentID);
            Assert.Equal(equipment, viewResult.ViewData["Equipment"]);
        }

        [Fact]
        public void Borrow_Post_ValidModel_RedirectsToMyEquipments()
        {
            // Arrange
            SetupUserContext("student-id", "Student");

            var model = new BorrowEquipmentViewModel
            {
                EquipmentID = "test-equipment-id",
                Quantity = 1,
                Notes = "Test borrowing"
            };

            _mockEquipmentHandler.Setup(handler => handler.BorrowEquipment(
                It.IsAny<string>(),
                It.Is<BorrowEquipmentDto>(dto =>
                    dto.EquipmentID == model.EquipmentID &&
                    dto.Quantity == model.Quantity &&
                    dto.Notes == model.Notes
                )
            )).Returns(new BorrowRecord());

            // Act
            var result = _controller.Borrow(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyEquipments", redirectResult.ActionName);
        }

        [Fact]
        public void Borrow_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            string equipmentId = "test-equipment-id";

            var model = new BorrowEquipmentViewModel
            {
                EquipmentID = equipmentId
            };

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                Name = "Test Equipment"
            };

            _controller.ModelState.AddModelError("Quantity", "Required");
            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns(equipment);

            // Act
            var result = _controller.Borrow(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal(equipment, viewResult.ViewData["Equipment"]);
        }

        [Fact]
        public void Borrow_Post_ThrowsException_ReturnsViewWithError()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            string equipmentId = "test-equipment-id";
            string errorMessage = "Error borrowing equipment";

            var model = new BorrowEquipmentViewModel
            {
                EquipmentID = equipmentId,
                Quantity = 1
            };

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                Name = "Test Equipment"
            };

            _mockEquipmentHandler.Setup(handler => handler.BorrowEquipment(
                It.IsAny<string>(),
                It.IsAny<BorrowEquipmentDto>()
            )).Throws(new Exception(errorMessage));

            _mockEquipmentHandler.Setup(handler => handler.GetEquipmentById(equipmentId))
                .Returns(equipment);

            // Act
            var result = _controller.Borrow(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal(equipment, viewResult.ViewData["Equipment"]);
            Assert.True(_controller.ModelState.ContainsKey(""));
            Assert.Equal(errorMessage, _controller.ModelState[""].Errors[0].ErrorMessage);
        }

        [Fact]
        public void MyEquipments_ReturnsViewWithRecords()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            var records = new List<BorrowRecord>
            {
                new BorrowRecord { RecordID = "record1", EquipmentID = "equip1" },
                new BorrowRecord { RecordID = "record2", EquipmentID = "equip2" }
            };

            _mockEquipmentHandler.Setup(handler => handler.GetStudentBorrowRecords("student-id"))
                .Returns(records);

            // Act
            var result = _controller.MyEquipments();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<BorrowRecord>>(viewResult.Model);
            Assert.Equal(records, model);
        }

        [Fact]
        public void Return_ValidRecordId_RedirectsToMyEquipments()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            string recordId = "test-record-id";

            _mockEquipmentHandler.Setup(handler => handler.ReturnEquipment("student-id", recordId))
                .Verifiable();

            // Act
            var result = _controller.Return(recordId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyEquipments", redirectResult.ActionName);
            _mockEquipmentHandler.Verify();
        }

        [Fact]
        public void Return_ThrowsException_ReturnsForbid()
        {
            // Arrange
            SetupUserContext("student-id", "Student");
            string recordId = "test-record-id";

            _mockEquipmentHandler.Setup(handler => handler.ReturnEquipment("student-id", recordId))
                .Throws(new Exception("Error returning equipment"));

            // Act
            var result = _controller.Return(recordId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
    }
}