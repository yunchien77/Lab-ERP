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
    public class WorkSessionControllerTests
    {
        private readonly Mock<IWorkSessionHandler> _mockWorkSessionHandler;
        //private readonly Mock<IUserHandler> _mockUserHandler;
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly WorkSessionController _controller;
        private readonly Mock<ITempDataProvider> _tempDataProvider;
        private readonly TempDataDictionary _tempData;

        public WorkSessionControllerTests()
        {
            _mockWorkSessionHandler = new Mock<IWorkSessionHandler>(MockBehavior.Strict);
           // _mockUserHandler = new Mock<IUserHandler>();
            _mockLabRepository = new Mock<ILaboratoryRepository>();
            _controller = new WorkSessionController(
                _mockWorkSessionHandler.Object,
                //_mockUserHandler.Object,
                _mockLabRepository.Object);

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

        private Laboratory CreateTestLaboratory(string labId = "test-lab", string creatorId = "professor-1")
        {
            var professor = new Professor
            {
                UserID = creatorId,
                Username = "Test Professor"
            };

            var student = new Student
            {
                UserID = "test-user",
                Username = "Test Student"
            };

            return new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = professor,
                Members = new List<User> { student }
            };
        }

        #region Index Tests

        [Fact]
        public void Index_ValidLabIdAndUserIsMember_ReturnsViewWithModel()
        {
            // Arrange
            SetupUserContext();
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId);
            var currentStatus = WorkStatus.NotStarted;

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockWorkSessionHandler.Setup(x => x.GetCurrentWorkStatus("test-user", labId)).Returns(currentStatus);

            // Act
            var result = _controller.Index(labId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<WorkSessionViewModel>(viewResult.Model);
            Assert.Equal(labId, model.LabID);
            Assert.Equal("Test Lab", model.LabName);
            Assert.Equal(currentStatus, model.CurrentStatus);
            Assert.Equal("test-user", model.StudentID);
        }

        [Fact]
        public void Index_LabNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext();
            var labId = "non-existent-lab";

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _controller.Index(labId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("實驗室不存在", notFoundResult.Value);
        }

        [Fact]
        public void Index_UserNotMemberOfLab_ReturnsForbid()
        {
            // Arrange
            SetupUserContext("non-member-user");
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId);

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);

            // Act
            var result = _controller.Index(labId);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        #endregion

        #region StartWork Tests

        [Fact]
        public void StartWork_Success_RedirectsWithSuccessMessage()
        {
            // Arrange
            SetupUserContext();
            var labId = "test-lab";
            var workSession = new WorkSession
            {
                StartTime = DateTime.Now,
                StudentID = "test-user",
                LabID = labId
            };

            _mockWorkSessionHandler.Setup(x => x.StartWork("test-user", labId)).Returns(workSession);

            // Act
            var result = _controller.StartWork(labId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(labId, redirectResult.RouteValues["labId"]);
            Assert.Contains("開始工作打卡成功", _tempData["SuccessMessage"].ToString());
        }

        [Fact]
        public void StartWork_ExceptionThrown_RedirectsWithErrorMessage()
        {
            // Arrange
            SetupUserContext();
            var labId = "test-lab";
            var exceptionMessage = "Cannot start work";

            _mockWorkSessionHandler.Setup(x => x.StartWork("test-user", labId))
                .Throws(new Exception(exceptionMessage));

            // Act
            var result = _controller.StartWork(labId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(labId, redirectResult.RouteValues["labId"]);
            Assert.Equal(exceptionMessage, _tempData["ErrorMessage"]);
        }

        #endregion

        #region EndWork Tests

        [Fact]
        public void EndWork_Success_RedirectsWithSuccessMessage()
        {
            // Arrange
            SetupUserContext();
            var labId = "test-lab";
            var workSession = new WorkSession
            {
                StartTime = DateTime.Now.AddHours(-2),
                EndTime = DateTime.Now,
                WorkDuration = TimeSpan.FromHours(2),
                StudentID = "test-user",
                LabID = labId
            };

            _mockWorkSessionHandler.Setup(x => x.EndWork("test-user", labId)).Returns(workSession);

            // Act
            var result = _controller.EndWork(labId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(labId, redirectResult.RouteValues["labId"]);
            Assert.Contains("結束工作打卡成功", _tempData["SuccessMessage"].ToString());
        }

        [Fact]
        public void EndWork_ExceptionThrown_RedirectsWithErrorMessage()
        {
            // Arrange
            SetupUserContext();
            var labId = "test-lab";
            var exceptionMessage = "Cannot end work";

            _mockWorkSessionHandler.Setup(x => x.EndWork("test-user", labId))
                .Throws(new Exception(exceptionMessage));

            // Act
            var result = _controller.EndWork(labId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal(labId, redirectResult.RouteValues["labId"]);
            Assert.Equal(exceptionMessage, _tempData["ErrorMessage"]);
        }

        #endregion

        #region Records Tests

        [Fact]
        public void Records_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            SetupUserContext();
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId);
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var workSessions = new List<WorkSession>
            {
                new WorkSession { StudentID = "test-user", LabID = labId }
            };
            var summary = new WorkSessionSummary
            {
                StudentId = "test-user",
                LabId = labId,
                TotalHours = 40.0
            };

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSessions("test-user", labId, startDate, endDate))
                .Returns(workSessions);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSummary("test-user", labId, startDate, endDate))
                .Returns(summary);

            // Act
            var result = _controller.Records(labId, startDate, endDate);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<WorkSessionRecordsViewModel>(viewResult.Model);
            Assert.Equal(labId, model.LabID);
            Assert.Equal("Test Lab", model.LabName);
            Assert.Equal(startDate, model.StartDate);
            Assert.Equal(endDate, model.EndDate);
            Assert.Single(model.WorkSessions);
            Assert.Equal(summary, model.Summary);
        }

        [Fact]
        public void Records_LabNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext();
            var labId = "non-existent-lab";

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _controller.Records(labId, null, null);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("實驗室不存在", notFoundResult.Value);
        }

        [Fact]
        public void Records_NoDatesProvided_UsesDefaultDates()
        {
            // Arrange
            SetupUserContext();
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId);
            var workSessions = new List<WorkSession>();
            var summary = new WorkSessionSummary();

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSessions(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(workSessions);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSummary(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(summary);

            // Act
            var result = _controller.Records(labId, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<WorkSessionRecordsViewModel>(viewResult.Model);

            // Check that default dates are current month
            var expectedStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var expectedEndDate = expectedStartDate.AddMonths(1).AddDays(-1);

            Assert.Equal(expectedStartDate, model.StartDate);
            Assert.Equal(expectedEndDate, model.EndDate);
        }

        #endregion

        #region LabAttendance Tests

        [Fact]
        public void LabAttendance_ValidProfessorRequest_ReturnsViewWithModel()
        {
            // Arrange
            SetupUserContext("professor-1", "Professor");
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId, "professor-1");
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var studentSummaries = new List<StudentWorkSummary>
            {
                new StudentWorkSummary
                {
                    StudentId = "student-1",
                    StudentName = "Test Student",
                    TotalHours = 40.0
                }
            };

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockWorkSessionHandler.Setup(x => x.GetLabWorkSummary(labId, startDate, endDate))
                .Returns(studentSummaries);

            // Act
            var result = _controller.LabAttendance(labId, startDate, endDate);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<LabAttendanceViewModel>(viewResult.Model);
            Assert.Equal(labId, model.LabID);
            Assert.Equal("Test Lab", model.LabName);
            Assert.Equal(startDate, model.StartDate);
            Assert.Equal(endDate, model.EndDate);
            Assert.Single(model.StudentSummaries);
        }

        [Fact]
        public void LabAttendance_LabNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("professor-1", "Professor");
            var labId = "non-existent-lab";

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _controller.LabAttendance(labId, null, null);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("實驗室不存在", notFoundResult.Value);
        }

        [Fact]
        public void LabAttendance_ProfessorNotCreator_ReturnsForbid()
        {
            // Arrange
            SetupUserContext("other-professor", "Professor");
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId, "professor-1");

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);

            // Act
            var result = _controller.LabAttendance(labId, null, null);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public void LabAttendance_NoDatesProvided_UsesDefaultDates()
        {
            // Arrange
            SetupUserContext("professor-1", "Professor");
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId, "professor-1");
            var studentSummaries = new List<StudentWorkSummary>();

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockWorkSessionHandler.Setup(x => x.GetLabWorkSummary(It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(studentSummaries);

            // Act
            var result = _controller.LabAttendance(labId, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<LabAttendanceViewModel>(viewResult.Model);

            // Check that default dates are current month
            var expectedStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var expectedEndDate = expectedStartDate.AddMonths(1).AddDays(-1);

            Assert.Equal(expectedStartDate, model.StartDate);
            Assert.Equal(expectedEndDate, model.EndDate);
        }

        #endregion

        #region StudentRecords Tests

        [Fact]
        public void StudentRecords_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            SetupUserContext("professor-1", "Professor");
            var labId = "test-lab";
            var studentId = "test-user";
            var laboratory = CreateTestLaboratory(labId, "professor-1");
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 1, 31);
            var workSessions = new List<WorkSession>
            {
                new WorkSession { StudentID = studentId, LabID = labId }
            };
            var summary = new WorkSessionSummary
            {
                StudentId = studentId,
                LabId = labId,
                TotalHours = 40.0
            };

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSessions(studentId, labId, startDate, endDate))
                .Returns(workSessions);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSummary(studentId, labId, startDate, endDate))
                .Returns(summary);

            // Act
            var result = _controller.StudentRecords(labId, studentId, startDate, endDate);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<StudentWorkRecordsViewModel>(viewResult.Model);
            Assert.Equal(labId, model.LabID);
            Assert.Equal("Test Lab", model.LabName);
            Assert.Equal(studentId, model.StudentID);
            Assert.Equal("Test Student", model.StudentName);
            Assert.Equal(startDate, model.StartDate);
            Assert.Equal(endDate, model.EndDate);
            Assert.Single(model.WorkSessions);
            Assert.Equal(summary, model.Summary);
        }

        [Fact]
        public void StudentRecords_LabNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("professor-1", "Professor");
            var labId = "non-existent-lab";

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _controller.StudentRecords(labId, "student-1", null, null);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("實驗室不存在", notFoundResult.Value);
        }

        [Fact]
        public void StudentRecords_ProfessorNotCreator_ReturnsForbid()
        {
            // Arrange
            SetupUserContext("other-professor", "Professor");
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId, "professor-1");

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);

            // Act
            var result = _controller.StudentRecords(labId, "student-1", null, null);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public void StudentRecords_StudentNotMember_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext("professor-1", "Professor");
            var labId = "test-lab";
            var laboratory = CreateTestLaboratory(labId, "professor-1");
            var nonMemberStudentId = "non-member-student";

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);

            // Act
            var result = _controller.StudentRecords(labId, nonMemberStudentId, null, null);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("學生不是此實驗室成員", notFoundResult.Value);
        }

        [Fact]
        public void StudentRecords_NoDatesProvided_UsesDefaultDates()
        {
            // Arrange
            SetupUserContext("professor-1", "Professor");
            var labId = "test-lab";
            var studentId = "test-user";
            var laboratory = CreateTestLaboratory(labId, "professor-1");
            var workSessions = new List<WorkSession>();
            var summary = new WorkSessionSummary();

            _mockLabRepository.Setup(x => x.GetById(labId)).Returns(laboratory);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSessions(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(workSessions);
            _mockWorkSessionHandler.Setup(x => x.GetWorkSummary(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(summary);

            // Act
            var result = _controller.StudentRecords(labId, studentId, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<StudentWorkRecordsViewModel>(viewResult.Model);

            // Check that default dates are current month
            var expectedStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var expectedEndDate = expectedStartDate.AddMonths(1).AddDays(-1);

            Assert.Equal(expectedStartDate, model.StartDate);
            Assert.Equal(expectedEndDate, model.EndDate);
        }

        #endregion
    }
}