using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;
using LabERP.Controllers;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.Handlers;
using LabERP.Models.ViewModels;

namespace LabERP.Tests.Controllers
{
    public class LaboratoryControllerTests
    {
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LaboratoryHandler _labHandler;
        private readonly LaboratoryController _controller;
        private readonly ClaimsPrincipal _user;
        private const string TestUserId = "testUserId";

        public LaboratoryControllerTests()
        {
            // 設置儲存庫和服務的模擬
            _mockLabRepository = new Mock<ILaboratoryRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockNotificationService = new Mock<INotificationService>();

            // 創建實際的 handler 使用模擬的相依服務
            _labHandler = new LaboratoryHandler(
                _mockLabRepository.Object,
                _mockUserRepository.Object,
                _mockNotificationService.Object);

            // 創建控制器
            _controller = new LaboratoryController(_labHandler);

            // 設置模擬用戶
            var claims = new List<Claim>
            {
                new Claim("UserID", TestUserId),
                new Claim(ClaimTypes.Role, "Professor")
            };
            _user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _user }
            };

            // 設置 TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        #region Create Tests

        [Fact]
        public void Create_Get_ReturnsViewResult()
        {
            // Act
            var result = _controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Create_Post_ValidModel_RedirectsToDetails()
        {
            // Arrange
            var model = new RegisterLabViewModel
            {
                Name = "測試實驗室",
                Description = "測試描述",
                Website = "http://test.com",
                ContactInfo = "test@example.com"
            };

            var professor = new Professor
            {
                UserID = TestUserId,
                Username = "TestProf",
                Email = "prof@example.com",
                Laboratories = new List<Laboratory>()
            };

            var createdLab = new Laboratory
            {
                LabID = "testLabId",
                Name = model.Name,
                Description = model.Description,
                Website = model.Website,
                ContactInfo = model.ContactInfo,
                Creator = professor
            };

            // 設置模擬行為
            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns(professor);

            _mockLabRepository.Setup(r => r.Add(It.IsAny<Laboratory>()))
                .Callback<Laboratory>(lab => { /* 可添加邏輯確保實驗室被正確構建 */ });

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(createdLab);

            // Act
            var result = _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.NotNull(redirectResult.RouteValues["id"]);
        }

        [Fact]
        public void Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new RegisterLabViewModel();
            _controller.ModelState.AddModelError("Name", "實驗室名稱為必填項");

            // Act
            var result = _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public void Create_Post_UserNotProfessor_ReturnsViewWithError()
        {
            // Arrange
            var model = new RegisterLabViewModel
            {
                Name = "測試實驗室",
                Description = "測試描述"
            };

            // 模擬 GetUserById 返回 null 以觸發錯誤
            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns((Professor)null);

            // Act
            var result = _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values, v => v.Errors.Count > 0);
        }

        #endregion

        #region Details Tests

        [Fact]
        public void Details_LabExists_ReturnsViewWithLab()
        {
            // Arrange
            var professor = new Professor { UserID = TestUserId, Username = "TestProf" };
            var lab = new Laboratory
            {
                LabID = "testLabId",
                Name = "測試實驗室",
                Creator = professor,
                Members = new List<User>()
            };

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(lab);

            // Act
            var result = _controller.Details("testLabId");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Laboratory>(viewResult.Model);
            Assert.Equal("testLabId", model.LabID);
            Assert.Equal("測試實驗室", model.Name);
            Assert.Same(professor, model.Creator);
        }

        [Fact]
        public void Details_LabDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockLabRepository.Setup(r => r.GetById("nonExistentId"))
                .Returns((Laboratory)null);

            // Act
            var result = _controller.Details("nonExistentId");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region RemoveMember Tests

        [Fact]
        public void RemoveMember_Success_RedirectsToDetails()
        {
            // Arrange
            string labId = "testLabId";
            string memberId = "testMemberId";

            var professor = new Professor { UserID = TestUserId };
            var lab = new Laboratory
            {
                LabID = labId,
                Creator = professor,
                Members = new List<User>
                {
                    new Student { UserID = memberId, Username = "TestStudent" }
                }
            };

            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns(professor);

            _mockLabRepository.Setup(r => r.GetById(labId))
                .Returns(lab);

            _mockLabRepository.Setup(r => r.Update(It.IsAny<Laboratory>()))
                .Callback<Laboratory>(updatedLab => { /* 可添加邏輯確認成員被刪除 */ });

            // Act
            var result = _controller.RemoveMember(labId, memberId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(labId, redirectResult.RouteValues["id"]);
        }

        [Fact]
        public void RemoveMember_NotLabCreator_ReturnsForbid()
        {
            // Arrange
            string labId = "testLabId";
            string memberId = "testMemberId";

            var differentProfessor = new Professor { UserID = "differentProfId" };
            var lab = new Laboratory
            {
                LabID = labId,
                Creator = differentProfessor
            };

            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns(new Professor { UserID = TestUserId });

            _mockLabRepository.Setup(r => r.GetById(labId))
                .Returns(lab);

            // Act
            var result = _controller.RemoveMember(labId, memberId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        #endregion

        #region Edit Tests

        [Fact]
        public void Edit_Get_LabExistsAndUserIsCreator_ReturnsViewWithModel()
        {
            // Arrange
            var professor = new Professor { UserID = TestUserId };
            var lab = new Laboratory
            {
                LabID = "testLabId",
                Name = "測試實驗室",
                Description = "原始描述",
                Website = "http://original.com",
                ContactInfo = "original@example.com",
                Creator = professor
            };

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(lab);

            // Act
            var result = _controller.Edit("testLabId");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditLabViewModel>(viewResult.Model);
            Assert.Equal("testLabId", model.LabID);
            Assert.Equal("測試實驗室", model.Name);
            Assert.Equal("原始描述", model.Description);
            Assert.Equal("http://original.com", model.Website);
            Assert.Equal("original@example.com", model.ContactInfo);
        }

        [Fact]
        public void Edit_Get_LabDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockLabRepository.Setup(r => r.GetById("nonExistentId"))
                .Returns((Laboratory)null);

            // Act
            var result = _controller.Edit("nonExistentId");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Edit_Get_UserNotCreator_ReturnsForbid()
        {
            // Arrange
            var otherProfessor = new Professor { UserID = "differentUserId" };
            var lab = new Laboratory
            {
                LabID = "testLabId",
                Name = "測試實驗室",
                Creator = otherProfessor
            };

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(lab);

            // Act
            var result = _controller.Edit("testLabId");

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public void Edit_Post_ValidModel_RedirectsToDetails()
        {
            // Arrange
            var model = new EditLabViewModel
            {
                LabID = "testLabId",
                Name = "更新實驗室",
                Description = "更新描述",
                Website = "http://updated.com",
                ContactInfo = "updated@example.com"
            };

            var professor = new Professor { UserID = TestUserId };
            var lab = new Laboratory
            {
                LabID = "testLabId",
                Name = "原始名稱",
                Creator = professor
            };

            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns(professor);

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(lab);

            _mockLabRepository.Setup(r => r.Update(It.IsAny<Laboratory>()))
                .Callback<Laboratory>(updatedLab =>
                {
                    // 可添加檢查邏輯確認更新正確發生
                    Assert.Equal(model.Name, updatedLab.Name);
                    Assert.Equal(model.Description, updatedLab.Description);
                });

            // Act
            var result = _controller.Edit(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(model.LabID, redirectResult.RouteValues["id"]);
        }

        [Fact]
        public void Edit_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new EditLabViewModel { LabID = "testLabId" };
            _controller.ModelState.AddModelError("Name", "名稱為必填項");

            // Act
            var result = _controller.Edit(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public void Edit_Post_NotLabCreator_ReturnsViewWithError()
        {
            // Arrange
            var model = new EditLabViewModel
            {
                LabID = "testLabId",
                Name = "更新實驗室"
            };

            var otherProfessor = new Professor { UserID = "differentUserId" };
            var lab = new Laboratory
            {
                LabID = "testLabId",
                Name = "原始名稱",
                Creator = otherProfessor
            };

            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns(new Professor { UserID = TestUserId });

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(lab);

            // Act
            var result = _controller.Edit(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion

        #region AddMember Tests

        [Fact]
        public void AddMember_Get_ReturnsViewWithModel()
        {
            // Arrange
            string labId = "testLabId";

            // Act
            var result = _controller.AddMember(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AddMemberViewModel>(viewResult.Model);
            Assert.Equal(labId, model.LabID);
        }

        [Fact]
        public void AddMember_Post_ValidModel_RedirectsToDetails()
        {
            // Arrange
            var model = new AddMemberViewModel
            {
                LabID = "testLabId",
                Username = "newStudent",
                Email = "student@example.com"
            };

            var professor = new Professor { UserID = TestUserId };
            var lab = new Laboratory
            {
                LabID = "testLabId",
                Name = "測試實驗室",
                Creator = professor,
                Members = new List<User>()
            };

            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns(professor);

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(lab);

            _mockLabRepository.Setup(r => r.Update(It.IsAny<Laboratory>()))
                .Callback<Laboratory>(updatedLab => { /* 驗證邏輯 */ });

            // Fix: Change the callback parameter type to match the Add method parameter type (User)
            _mockUserRepository.Setup(r => r.Add(It.IsAny<User>()))
                .Callback<User>(user =>
                {
                    // We can still check the properties but need to cast if using Student-specific properties
                    Assert.Equal(model.Username, user.Username);
                    Assert.Equal(model.Email, user.Email);
                    Assert.Equal("Student", user.Role);
                });

            _mockNotificationService.Setup(n => n.NotifyNewMember(
                It.IsAny<Student>(),
                It.IsAny<object>()))
                .Verifiable();

            // Act
            var result = _controller.AddMember(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(model.LabID, redirectResult.RouteValues["id"]);
            _mockNotificationService.Verify(n => n.NotifyNewMember(
                It.IsAny<Student>(),
                It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public void AddMember_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new AddMemberViewModel { LabID = "testLabId" };
            _controller.ModelState.AddModelError("Username", "使用者名稱為必填項");

            // Act
            var result = _controller.AddMember(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public void AddMember_Post_NotLabCreator_ReturnsViewWithError()
        {
            // Arrange
            var model = new AddMemberViewModel
            {
                LabID = "testLabId",
                Username = "newStudent",
                Email = "student@example.com"
            };

            var otherProfessor = new Professor { UserID = "differentUserId" };
            var lab = new Laboratory
            {
                LabID = "testLabId",
                Name = "測試實驗室",
                Creator = otherProfessor
            };

            _mockUserRepository.Setup(r => r.GetUserById(TestUserId))
                .Returns(new Professor { UserID = TestUserId });

            _mockLabRepository.Setup(r => r.GetById("testLabId"))
                .Returns(lab);

            // Act
            var result = _controller.AddMember(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion
    }
}