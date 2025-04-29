using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;
using LabERP.Controllers;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.ViewModels;

namespace LabERP.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserHandler> _mockUserHandler;
        private readonly UserController _controller;
        private readonly string _testUserId = "test-user-id";

        public UserControllerTests()
        {
            _mockUserHandler = new Mock<IUserHandler>();
            _controller = new UserController(_mockUserHandler.Object);

            // Setup controller with HttpContext, TempData, etc.
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            // Setup TempData provider
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        private void SetupUserContext(string userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim("UserID", userId),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public void Dashboard_StudentUser_ReturnsViewWithStudentLaboratories()
        {
            // Arrange
            var userId = _testUserId;
            var student = new Student { UserID = userId };
            var labs = new List<Laboratory>
            {
                new Laboratory { LabID = "lab1", Name = "Test Lab 1" },
                new Laboratory { LabID = "lab2", Name = "Test Lab 2" }
            };

            SetupUserContext(userId, "Student");
            _mockUserHandler.Setup(x => x.GetUserById(userId)).Returns(student);
            _mockUserHandler.Setup(x => x.GetStudentLaboratories(userId)).Returns(labs);

            // Act
            var result = _controller.Dashboard() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(student, result.Model);
            Assert.Equal(labs, result.ViewData["StudentLaboratories"]);
        }

        [Fact]
        public void Dashboard_ProfessorUser_ReturnsViewWithoutStudentLaboratories()
        {
            // Arrange
            var userId = _testUserId;
            var professor = new User { UserID = userId, Role = "Professor" };

            SetupUserContext(userId, "Professor");
            _mockUserHandler.Setup(x => x.GetUserById(userId)).Returns(professor);

            // Act
            var result = _controller.Dashboard() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(professor, result.Model);
            Assert.Null(result.ViewData["StudentLaboratories"]);
        }

        [Fact]
        public void ChangePassword_Get_ReturnsView()
        {
            // Act
            var result = _controller.ChangePassword() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ChangePassword_Post_ValidData_ReturnsViewWithSuccessMessage()
        {
            // Arrange
            var userId = _testUserId;
            var oldPassword = "OldPass123";
            var newPassword = "NewPass456";

            SetupUserContext(userId, "Student");
            _mockUserHandler.Setup(x => x.ChangePassword(userId, oldPassword, newPassword)).Returns(true);

            // Act
            var result = _controller.ChangePassword(oldPassword, newPassword) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("密碼已成功修改。", result.ViewData["Message"]);
            _mockUserHandler.Verify(x => x.ChangePassword(userId, oldPassword, newPassword), Times.Once);
        }

        [Fact]
        public void ChangePassword_Post_InvalidOldPassword_ReturnsViewWithError()
        {
            // Arrange
            var userId = _testUserId;
            var oldPassword = "WrongOldPass";
            var newPassword = "NewPass456";

            SetupUserContext(userId, "Student");
            _mockUserHandler.Setup(x => x.ChangePassword(userId, oldPassword, newPassword)).Returns(false);

            // Act
            var result = _controller.ChangePassword(oldPassword, newPassword) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(_controller.ModelState.ErrorCount > 0);
            Assert.Contains(_controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage == "舊密碼不正確。"));
            _mockUserHandler.Verify(x => x.ChangePassword(userId, oldPassword, newPassword), Times.Once);
        }

        [Fact]
        public void ChangePassword_Post_ModelStateInvalid_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Error", "Model state is invalid");

            // Act
            var result = _controller.ChangePassword("oldpass", "newpass") as ViewResult;

            // Assert
            Assert.NotNull(result);
            _mockUserHandler.Verify(x => x.ChangePassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void EditProfile_Get_StudentRole_ReturnsViewWithModel()
        {
            // Arrange
            var userId = _testUserId;
            var student = new Student
            {
                UserID = userId,
                StudentID = "S12345",
                PhoneNumber = "123-456-7890"
            };

            SetupUserContext(userId, "Student");
            _mockUserHandler.Setup(x => x.GetUserById(userId)).Returns(student);

            // Act
            var result = _controller.EditProfile() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as EditProfileViewModel;
            Assert.NotNull(model);
            Assert.Equal(student.StudentID, model.StudentID);
            Assert.Equal(student.PhoneNumber, model.PhoneNumber);
        }

        [Fact]
        public void EditProfile_Get_NonStudentUser_ReturnsNotFound()
        {
            // Arrange
            var userId = _testUserId;
            var professor = new User { UserID = userId, Role = "Professor" };

            SetupUserContext(userId, "Student"); // Set role as Student, but return Professor object
            _mockUserHandler.Setup(x => x.GetUserById(userId)).Returns(professor);

            // Act
            var result = _controller.EditProfile();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void EditProfile_Post_ValidModel_ReturnsViewWithSuccessMessage()
        {
            // Arrange
            var userId = _testUserId;
            var model = new EditProfileViewModel
            {
                StudentID = "S12345",
                PhoneNumber = "123-456-7890"
            };

            SetupUserContext(userId, "Student");
            _mockUserHandler.Setup(x => x.UpdateStudentProfile(userId, It.IsAny<StudentProfileDto>()))
                .Returns(true)
                .Callback<string, StudentProfileDto>((id, dto) =>
                {
                    Assert.Equal(model.StudentID, dto.StudentID);
                    Assert.Equal(model.PhoneNumber, dto.PhoneNumber);
                });

            // Act
            var result = _controller.EditProfile(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("個人資料已成功更新。", result.ViewData["Message"]);
            Assert.Equal(model, result.Model);
            _mockUserHandler.Verify(x => x.UpdateStudentProfile(userId, It.IsAny<StudentProfileDto>()), Times.Once);
        }

        [Fact]
        public void EditProfile_Post_ModelStateInvalid_ReturnsView()
        {
            // Arrange
            var model = new EditProfileViewModel
            {
                StudentID = "S12345",
                PhoneNumber = "123-456-7890"
            };
            _controller.ModelState.AddModelError("Error", "Model state is invalid");

            // Act
            var result = _controller.EditProfile(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            _mockUserHandler.Verify(x => x.UpdateStudentProfile(It.IsAny<string>(), It.IsAny<StudentProfileDto>()), Times.Never);
        }

        [Fact]
        public void EditProfile_Post_UpdateFailed_ReturnsView()
        {
            // Arrange
            var userId = _testUserId;
            var model = new EditProfileViewModel
            {
                StudentID = "S12345",
                PhoneNumber = "123-456-7890"
            };

            SetupUserContext(userId, "Student");
            _mockUserHandler.Setup(x => x.UpdateStudentProfile(userId, It.IsAny<StudentProfileDto>())).Returns(false);

            // Act
            var result = _controller.EditProfile(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            Assert.Null(result.ViewData["Message"]);
            _mockUserHandler.Verify(x => x.UpdateStudentProfile(userId, It.IsAny<StudentProfileDto>()), Times.Once);
        }
    }
}