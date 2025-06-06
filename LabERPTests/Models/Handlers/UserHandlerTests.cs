using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.Handlers;

namespace LabERP.Tests.Handlers
{
    public class UserHandlerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly UserHandler _handler;

        public UserHandlerTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLabRepository = new Mock<ILaboratoryRepository>();
            
            _handler = new UserHandler(
                _mockUserRepository.Object,
                _mockLabRepository.Object
            );
        }

        #region GetUserById Tests

        [Fact]
        public void GetUserById_WithValidId_ReturnsUser()
        {
            // Arrange
            var userId = "user123";
            var user = new User
            {
                UserID = userId,
                Username = "testUser",
                Email = "user@test.com"
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(user);

            // Act
            var result = _handler.GetUserById(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user, result);
            Assert.Equal(userId, result.UserID);
            Assert.Equal("testUser", result.Username);
        }

        [Fact]
        public void GetUserById_WithNonexistentId_ReturnsNull()
        {
            // Arrange
            var userId = "nonexistent";
            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns((User)null);

            // Act
            var result = _handler.GetUserById(userId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public void ChangePassword_WithValidCredentials_UpdatesPasswordAndReturnsTrue()
        {
            // Arrange
            var userId = "user123";
            var oldPassword = "oldPass123";
            var newPassword = "newPass456";
            
            var user = new User
            {
                UserID = userId,
                Username = "testUser",
                Password = oldPassword
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(user);
            _mockUserRepository.Setup(r => r.Update(It.IsAny<User>())).Verifiable();

            // Act
            var result = _handler.ChangePassword(userId, oldPassword, newPassword);

            // Assert
            Assert.True(result);
            Assert.Equal(newPassword, user.Password);
            _mockUserRepository.Verify(r => r.Update(user), Times.Once);
        }

        [Fact]
        public void ChangePassword_WithInvalidOldPassword_ReturnsFalse()
        {
            // Arrange
            var userId = "user123";
            var oldPassword = "oldPass123";
            var wrongPassword = "wrongPass";
            var newPassword = "newPass456";
            
            var user = new User
            {
                UserID = userId,
                Username = "testUser",
                Password = oldPassword
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(user);

            // Act
            var result = _handler.ChangePassword(userId, wrongPassword, newPassword);

            // Assert
            Assert.False(result);
            Assert.Equal(oldPassword, user.Password); // Password should not change
            _mockUserRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public void ChangePassword_WithNonexistentUser_ReturnsFalse()
        {
            // Arrange
            var userId = "nonexistent";
            var oldPassword = "oldPass123";
            var newPassword = "newPass456";
            
            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns((User)null);

            // Act
            var result = _handler.ChangePassword(userId, oldPassword, newPassword);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        #endregion

        #region UpdateStudentProfile Tests

        [Fact]
        public void UpdateStudentProfile_WithValidStudentAndData_UpdatesProfileAndReturnsTrue()
        {
            // Arrange
            var studentId = "student123";
            var student = new Student
            {
                UserID = studentId,
                Username = "testStudent",
                StudentID = "S001",
                PhoneNumber = "1234567890"
            };
            
            var profileInfo = new StudentProfileDto
            {
                StudentID = "S002",
                PhoneNumber = "0987654321"
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockUserRepository.Setup(r => r.Update(It.IsAny<Student>())).Verifiable();

            // Act
            var result = _handler.UpdateStudentProfile(studentId, profileInfo);

            // Assert
            Assert.True(result);
            Assert.Equal(profileInfo.StudentID, student.StudentID);
            Assert.Equal(profileInfo.PhoneNumber, student.PhoneNumber);
            _mockUserRepository.Verify(r => r.Update(student), Times.Once);
        }

        [Fact]
        public void UpdateStudentProfile_WithNonStudentUser_ReturnsFalse()
        {
            // Arrange
            var userId = "prof123";
            var professor = new Professor
            {
                UserID = userId,
                Username = "testProf"
            };
            
            var profileInfo = new StudentProfileDto
            {
                StudentID = "S002",
                PhoneNumber = "0987654321"
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(professor);

            // Act
            var result = _handler.UpdateStudentProfile(userId, profileInfo);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public void UpdateStudentProfile_WithNonexistentUser_ReturnsFalse()
        {
            // Arrange
            var studentId = "nonexistent";
            var profileInfo = new StudentProfileDto
            {
                StudentID = "S002",
                PhoneNumber = "0987654321"
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns((User)null);

            // Act
            var result = _handler.UpdateStudentProfile(studentId, profileInfo);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        #endregion

        #region GetStudentLaboratories Tests

        [Fact]
        public void GetStudentLaboratories_WithMembershipInLabs_ReturnsLaboratories()
        {
            // Arrange
            var studentId = "student123";
            var student = new Student
            {
                UserID = studentId,
                Username = "testStudent"
            };
            
            var professor = new Professor
            {
                UserID = "prof123",
                Username = "testProf"
            };
            
            var lab1 = new Laboratory
            {
                LabID = "lab1",
                Name = "Lab 1",
                Creator = professor,
                Members = new List<User> { student }
            };
            
            var lab2 = new Laboratory
            {
                LabID = "lab2",
                Name = "Lab 2",
                Creator = professor,
                Members = new List<User> { student }
            };
            
            var lab3 = new Laboratory
            {
                LabID = "lab3",
                Name = "Lab 3",
                Creator = professor,
                Members = new List<User>() // Empty members list
            };
            
            var allLabs = new List<Laboratory> { lab1, lab2, lab3 };
            
            _mockLabRepository.Setup(r => r.GetAll()).Returns(allLabs);

            // Act
            var result = _handler.GetStudentLaboratories(studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(lab1, result);
            Assert.Contains(lab2, result);
            Assert.DoesNotContain(lab3, result);
        }

        [Fact]
        public void GetStudentLaboratories_WithNoMembership_ReturnsEmptyCollection()
        {
            // Arrange
            var studentId = "student123";
            var professor = new Professor
            {
                UserID = "prof123",
                Username = "testProf"
            };
            
            var otherStudent = new Student
            {
                UserID = "student456",
                Username = "otherStudent"
            };
            
            var lab1 = new Laboratory
            {
                LabID = "lab1",
                Name = "Lab 1",
                Creator = professor,
                Members = new List<User> { otherStudent }
            };
            
            var lab2 = new Laboratory
            {
                LabID = "lab2",
                Name = "Lab 2",
                Creator = professor,
                Members = new List<User>()
            };
            
            var allLabs = new List<Laboratory> { lab1, lab2 };
            
            _mockLabRepository.Setup(r => r.GetAll()).Returns(allLabs);

            // Act
            var result = _handler.GetStudentLaboratories(studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetStudentLaboratories_WithNoLabs_ReturnsEmptyCollection()
        {
            // Arrange
            var studentId = "student123";
            var emptyList = new List<Laboratory>();
            
            _mockLabRepository.Setup(r => r.GetAll()).Returns(emptyList);

            // Act
            var result = _handler.GetStudentLaboratories(studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}