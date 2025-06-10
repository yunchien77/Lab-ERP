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
    public class WorkSessionHandlerTests
    {
        private readonly Mock<IWorkSessionRepository> _mockWorkSessionRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly WorkSessionHandler _workSessionHandler;

        public WorkSessionHandlerTests()
        {
            _mockWorkSessionRepository = new Mock<IWorkSessionRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLabRepository = new Mock<ILaboratoryRepository>();
            _workSessionHandler = new WorkSessionHandler(
                _mockWorkSessionRepository.Object,
                _mockUserRepository.Object,
                _mockLabRepository.Object);
        }

        #region StartWork Tests

        [Fact]
        public void StartWork_ValidStudentAndLab_CreatesWorkSession()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            var student = new Student { UserID = studentId, Username = "TestStudent" };
            var professor = new Professor { UserID = "prof123" };
            var lab = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = professor,
                Members = new List<User> { student }
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);
            _mockWorkSessionRepository.Setup(r => r.GetCurrentWorkingSession(studentId, labId)).Returns((WorkSession)null);
            _mockWorkSessionRepository.Setup(r => r.Add(It.IsAny<WorkSession>())).Returns((WorkSession ws) => ws);

            // Act
            var result = _workSessionHandler.StartWork(studentId, labId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(studentId, result.StudentID);
            Assert.Equal(labId, result.LabID);
            Assert.Equal(WorkStatus.Working, result.Status);
            Assert.Equal(student, result.Student);
            Assert.Equal(lab, result.Laboratory);
            _mockWorkSessionRepository.Verify(r => r.Add(It.IsAny<WorkSession>()), Times.Once);
        }

        [Fact]
        public void StartWork_StudentNotExists_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "nonexistent";
            string labId = "lab123";

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns((User)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _workSessionHandler.StartWork(studentId, labId));
            Assert.Equal("學生不存在", exception.Message);
        }

        [Fact]
        public void StartWork_LabNotExists_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string labId = "nonexistent";
            var student = new Student { UserID = studentId };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _workSessionHandler.StartWork(studentId, labId));
            Assert.Equal("實驗室不存在", exception.Message);
        }

        [Fact]
        public void StartWork_StudentNotLabMember_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            var student = new Student { UserID = studentId };
            var otherStudent = new Student { UserID = "other123" };
            var lab = new Laboratory
            {
                LabID = labId,
                Members = new List<User> { otherStudent } // student is not in members
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _workSessionHandler.StartWork(studentId, labId));
            Assert.Equal("您不是此實驗室的成員", exception.Message);
        }

        [Fact]
        public void StartWork_AlreadyWorking_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            var student = new Student { UserID = studentId };
            var lab = new Laboratory
            {
                LabID = labId,
                Members = new List<User> { student }
            };
            var existingSession = new WorkSession { StudentID = studentId, LabID = labId, Status = WorkStatus.Working };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);
            _mockWorkSessionRepository.Setup(r => r.GetCurrentWorkingSession(studentId, labId)).Returns(existingSession);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _workSessionHandler.StartWork(studentId, labId));
            Assert.Equal("您已經在工作中，無法重複開始工作", exception.Message);
        }

        #endregion

        #region EndWork Tests

        [Fact]
        public void EndWork_ValidSession_EndsWorkSession()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            var workSession = new WorkSession
            {
                StudentID = studentId,
                LabID = labId,
                Status = WorkStatus.Working,
                StartTime = DateTime.Now.AddHours(-2)
            };

            _mockWorkSessionRepository.Setup(r => r.GetCurrentWorkingSession(studentId, labId)).Returns(workSession);
            _mockWorkSessionRepository.Setup(r => r.Update(It.IsAny<WorkSession>())).Returns((WorkSession ws) => ws);

            // Act
            var result = _workSessionHandler.EndWork(studentId, labId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(WorkStatus.Completed, result.Status);
            Assert.NotNull(result.EndTime);
            Assert.NotNull(result.WorkDuration);
            _mockWorkSessionRepository.Verify(r => r.Update(It.IsAny<WorkSession>()), Times.Once);
        }

        [Fact]
        public void EndWork_NoCurrentSession_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";

            _mockWorkSessionRepository.Setup(r => r.GetCurrentWorkingSession(studentId, labId)).Returns((WorkSession)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _workSessionHandler.EndWork(studentId, labId));
            Assert.Equal("您尚未開始工作，無法結束工作", exception.Message);
        }

        #endregion

        #region GetCurrentWorkStatus Tests

        [Fact]
        public void GetCurrentWorkStatus_HasCurrentSession_ReturnsWorkingStatus()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            var workSession = new WorkSession { Status = WorkStatus.Working };

            _mockWorkSessionRepository.Setup(r => r.GetCurrentWorkingSession(studentId, labId)).Returns(workSession);

            // Act
            var result = _workSessionHandler.GetCurrentWorkStatus(studentId, labId);

            // Assert
            Assert.Equal(WorkStatus.Working, result);
        }

        [Fact]
        public void GetCurrentWorkStatus_NoCurrentSession_ReturnsNotStarted()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";

            _mockWorkSessionRepository.Setup(r => r.GetCurrentWorkingSession(studentId, labId)).Returns((WorkSession)null);

            // Act
            var result = _workSessionHandler.GetCurrentWorkStatus(studentId, labId);

            // Assert
            Assert.Equal(WorkStatus.NotStarted, result);
        }

        #endregion

        #region GetWorkSessions Tests

        [Fact]
        public void GetWorkSessions_WithDateRange_ReturnsFilteredSessions()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-7);
            DateTime endDate = DateTime.Now;
            var expectedSessions = new List<WorkSession>
            {
                new WorkSession { StudentID = studentId, LabID = labId }
            };

            _mockWorkSessionRepository.Setup(r => r.GetByDateRange(studentId, labId, startDate, endDate))
                .Returns(expectedSessions);

            // Act
            var result = _workSessionHandler.GetWorkSessions(studentId, labId, startDate, endDate);

            // Assert
            Assert.Equal(expectedSessions, result);
            _mockWorkSessionRepository.Verify(r => r.GetByDateRange(studentId, labId, startDate, endDate), Times.Once);
        }

        [Fact]
        public void GetWorkSessions_WithoutDateRange_ReturnsAllSessions()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            var expectedSessions = new List<WorkSession>
            {
                new WorkSession { StudentID = studentId, LabID = labId },
                new WorkSession { StudentID = studentId, LabID = labId }
            };

            _mockWorkSessionRepository.Setup(r => r.GetByStudentAndLab(studentId, labId))
                .Returns(expectedSessions);

            // Act
            var result = _workSessionHandler.GetWorkSessions(studentId, labId);

            // Assert
            Assert.Equal(expectedSessions, result);
            _mockWorkSessionRepository.Verify(r => r.GetByStudentAndLab(studentId, labId), Times.Once);
        }

        [Fact]
        public void GetWorkSessions_OnlyStartDate_ReturnsAllSessions()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-7);
            var expectedSessions = new List<WorkSession>
            {
                new WorkSession { StudentID = studentId, LabID = labId }
            };

            _mockWorkSessionRepository.Setup(r => r.GetByStudentAndLab(studentId, labId))
                .Returns(expectedSessions);

            // Act
            var result = _workSessionHandler.GetWorkSessions(studentId, labId, startDate, null);

            // Assert
            Assert.Equal(expectedSessions, result);
            _mockWorkSessionRepository.Verify(r => r.GetByStudentAndLab(studentId, labId), Times.Once);
        }

        #endregion

        #region GetWorkSummary Tests

        [Fact]
        public void GetWorkSummary_WithCompletedSessions_ReturnsCorrectSummary()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-7);
            DateTime endDate = DateTime.Now;

            var student = new Student { UserID = studentId, Username = "TestStudent" };
            var lab = new Laboratory { LabID = labId, Name = "TestLab" };

            var sessions = new List<WorkSession>
            {
                new WorkSession
                {
                    StudentID = studentId,
                    LabID = labId,
                    Status = WorkStatus.Completed,
                    StartTime = DateTime.Now.AddHours(-4),
                    EndTime = DateTime.Now.AddHours(-2),
                    WorkDuration = TimeSpan.FromHours(2)
                },
                new WorkSession
                {
                    StudentID = studentId,
                    LabID = labId,
                    Status = WorkStatus.Completed,
                    StartTime = DateTime.Now.AddHours(-6),
                    EndTime = DateTime.Now.AddHours(-3),
                    WorkDuration = TimeSpan.FromHours(3)
                },
                new WorkSession
                {
                    StudentID = studentId,
                    LabID = labId,
                    Status = WorkStatus.Working // This should be filtered out
                }
            };

            _mockWorkSessionRepository.Setup(r => r.GetByDateRange(studentId, labId, startDate, endDate))
                .Returns(sessions);
            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act
            var result = _workSessionHandler.GetWorkSummary(studentId, labId, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(studentId, result.StudentId);
            Assert.Equal("TestStudent", result.StudentName);
            Assert.Equal(labId, result.LabId);
            Assert.Equal("TestLab", result.LabName);
            Assert.Equal(startDate, result.StartDate);
            Assert.Equal(endDate, result.EndDate);
            Assert.Equal(5.0, result.TotalHours); // 2 + 3 hours
            Assert.Equal(2, result.TotalSessions); // Only completed sessions
            Assert.Equal(2.5, result.AverageHoursPerSession); // 5/2
        }

        [Fact]
        public void GetWorkSummary_NoSessions_ReturnsZeroSummary()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-7);
            DateTime endDate = DateTime.Now;

            var student = new Student { UserID = studentId, Username = "TestStudent" };
            var lab = new Laboratory { LabID = labId, Name = "TestLab" };

            _mockWorkSessionRepository.Setup(r => r.GetByDateRange(studentId, labId, startDate, endDate))
                .Returns(new List<WorkSession>());
            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act
            var result = _workSessionHandler.GetWorkSummary(studentId, labId, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0.0, result.TotalHours);
            Assert.Equal(0, result.TotalSessions);
            Assert.Equal(0.0, result.AverageHoursPerSession);
        }

        [Fact]
        public void GetWorkSummary_StudentNotFound_ReturnsUnknownName()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-7);
            DateTime endDate = DateTime.Now;

            var lab = new Laboratory { LabID = labId, Name = "TestLab" };

            _mockWorkSessionRepository.Setup(r => r.GetByDateRange(studentId, labId, startDate, endDate))
                .Returns(new List<WorkSession>());
            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns((User)null);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act
            var result = _workSessionHandler.GetWorkSummary(studentId, labId, startDate, endDate);

            // Assert
            Assert.Equal("未知", result.StudentName);
        }

        [Fact]
        public void GetWorkSummary_LabNotFound_ReturnsUnknownName()
        {
            // Arrange
            string studentId = "student123";
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-7);
            DateTime endDate = DateTime.Now;

            var student = new Student { UserID = studentId, Username = "TestStudent" };

            _mockWorkSessionRepository.Setup(r => r.GetByDateRange(studentId, labId, startDate, endDate))
                .Returns(new List<WorkSession>());
            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _workSessionHandler.GetWorkSummary(studentId, labId, startDate, endDate);

            // Assert
            Assert.Equal("未知", result.LabName);
        }

        #endregion

        #region GetLabWorkSummary Tests

        [Fact]
        public void GetLabWorkSummary_ValidLab_ReturnsStudentSummaries()
        {
            // Arrange
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now;
            var weekStart = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);

            var student1 = new Student { UserID = "student1", Username = "Student1" };
            var student2 = new Student { UserID = "student2", Username = "Student2" };
            var professor = new Professor { UserID = "prof123" };

            var lab = new Laboratory
            {
                LabID = labId,
                Name = "TestLab",
                Creator = professor,
                Members = new List<User> { student1, student2, professor }
            };

            var student1Sessions = new List<WorkSession>
            {
                new WorkSession
                {
                    StudentID = "student1",
                    LabID = labId,
                    Status = WorkStatus.Completed,
                    StartTime = DateTime.Now.AddDays(-1),
                    WorkDuration = TimeSpan.FromHours(2)
                }
            };

            var student1WeekSessions = new List<WorkSession>
            {
                new WorkSession { StudentID = "student1", LabID = labId }
            };

            var student2Sessions = new List<WorkSession>
            {
                new WorkSession
                {
                    StudentID = "student2",
                    LabID = labId,
                    Status = WorkStatus.Completed,
                    StartTime = DateTime.Now.AddDays(-2),
                    WorkDuration = TimeSpan.FromHours(3)
                }
            };

            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            _mockWorkSessionRepository.Setup(r => r.GetByDateRange("student1", labId, startDate, endDate))
                .Returns(student1Sessions);
            _mockWorkSessionRepository.Setup(r => r.GetByDateRange("student1", labId, weekStart, It.IsAny<DateTime>()))
                .Returns(student1WeekSessions);

            _mockWorkSessionRepository.Setup(r => r.GetByDateRange("student2", labId, startDate, endDate))
                .Returns(student2Sessions);
            _mockWorkSessionRepository.Setup(r => r.GetByDateRange("student2", labId, weekStart, It.IsAny<DateTime>()))
                .Returns(new List<WorkSession>()); // No work this week

            // Act
            var result = _workSessionHandler.GetLabWorkSummary(labId, startDate, endDate).ToList();

            // Assert
            Assert.Equal(2, result.Count);

            var student1Summary = result.FirstOrDefault(s => s.StudentId == "student1");
            Assert.NotNull(student1Summary);
            Assert.Equal("Student1", student1Summary.StudentName);
            Assert.Equal(2.0, student1Summary.TotalHours);
            Assert.Equal(1, student1Summary.TotalSessions);
            Assert.True(student1Summary.HasWorkedThisWeek);
            Assert.NotNull(student1Summary.LastWorkDate);

            var student2Summary = result.FirstOrDefault(s => s.StudentId == "student2");
            Assert.NotNull(student2Summary);
            Assert.Equal("Student2", student2Summary.StudentName);
            Assert.Equal(3.0, student2Summary.TotalHours);
            Assert.Equal(1, student2Summary.TotalSessions);
            Assert.False(student2Summary.HasWorkedThisWeek);
        }

        [Fact]
        public void GetLabWorkSummary_LabNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            string labId = "nonexistent";
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now;

            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _workSessionHandler.GetLabWorkSummary(labId, startDate, endDate));
            Assert.Equal("實驗室不存在", exception.Message);
        }

        [Fact]
        public void GetLabWorkSummary_NoStudentMembers_ReturnsEmptyList()
        {
            // Arrange
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now;

            var professor = new Professor { UserID = "prof123" };
            var lab = new Laboratory
            {
                LabID = labId,
                Name = "TestLab",
                Creator = professor,
                Members = new List<User> { professor } // Only professor, no students
            };

            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act
            var result = _workSessionHandler.GetLabWorkSummary(labId, startDate, endDate).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetLabWorkSummary_StudentWithNoSessions_ReturnsZeroSummary()
        {
            // Arrange
            string labId = "lab123";
            DateTime startDate = DateTime.Now.AddDays(-30);
            DateTime endDate = DateTime.Now;

            var student = new Student { UserID = "student1", Username = "Student1" };
            var professor = new Professor { UserID = "prof123" };

            var lab = new Laboratory
            {
                LabID = labId,
                Name = "TestLab",
                Creator = professor,
                Members = new List<User> { student }
            };

            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);
            _mockWorkSessionRepository.Setup(r => r.GetByDateRange("student1", labId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<WorkSession>());

            // Act
            var result = _workSessionHandler.GetLabWorkSummary(labId, startDate, endDate).ToList();

            // Assert
            Assert.Single(result);
            var summary = result.First();
            Assert.Equal("student1", summary.StudentId);
            Assert.Equal("Student1", summary.StudentName);
            Assert.Equal(0.0, summary.TotalHours);
            Assert.Equal(0, summary.TotalSessions);
            Assert.Null(summary.LastWorkDate);
            Assert.False(summary.HasWorkedThisWeek);
        }

        #endregion
    }
}