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
    public class LaboratoryHandlerTests
    {
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LaboratoryHandler _handler;

        public LaboratoryHandlerTests()
        {
            _mockLabRepository = new Mock<ILaboratoryRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockNotificationService = new Mock<INotificationService>();

            _handler = new LaboratoryHandler(
                _mockLabRepository.Object,
                _mockUserRepository.Object,
                _mockNotificationService.Object
            );
        }

        #region CreateLaboratory Tests

        [Fact]
        public void CreateLaboratory_WithValidProfessor_CreatesAndReturnsLaboratory()
        {
            // Arrange
            var professorId = "prof123";
            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf",
                Email = "prof@test.com",
                Laboratories = new List<Laboratory>()
            };

            var labInfo = new LaboratoryCreateDto
            {
                Name = "Test Lab",
                Description = "Test Description",
                Website = "http://testlab.com",
                ContactInfo = "test@lab.com"
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.Add(It.IsAny<Laboratory>())).Verifiable();
            _mockUserRepository.Setup(r => r.Update(It.IsAny<Professor>())).Verifiable();

            // Act
            var result = _handler.CreateLaboratory(professorId, labInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(labInfo.Name, result.Name);
            Assert.Equal(labInfo.Description, result.Description);
            Assert.Equal(labInfo.Website, result.Website);
            Assert.Equal(labInfo.ContactInfo, result.ContactInfo);
            Assert.Equal(professor, result.Creator);

            _mockLabRepository.Verify(r => r.Add(It.IsAny<Laboratory>()), Times.Once);
            _mockUserRepository.Verify(r => r.Update(It.IsAny<Professor>()), Times.Once);
            Assert.Contains(result, professor.Laboratories);
        }

        [Fact]
        public void CreateLaboratory_WithNonProfessorUser_ThrowsException()
        {
            // Arrange
            var userId = "student123";
            var student = new Student
            {
                UserID = userId,
                Username = "testStudent",
                Email = "student@test.com"
            };

            var labInfo = new LaboratoryCreateDto
            {
                Name = "Test Lab"
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(student);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.CreateLaboratory(userId, labInfo)
            );

            Assert.Equal("只有教授可以創建實驗室", exception.Message);
            _mockLabRepository.Verify(r => r.Add(It.IsAny<Laboratory>()), Times.Never);
        }

        [Fact]
        public void CreateLaboratory_WithNullUser_ThrowsException()
        {
            // Arrange
            var userId = "nonexistent";
            var labInfo = new LaboratoryCreateDto
            {
                Name = "Test Lab"
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns((User)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.CreateLaboratory(userId, labInfo)
            );

            Assert.Equal("只有教授可以創建實驗室", exception.Message);
        }

        #endregion

        #region UpdateLaboratory Tests

        [Fact]
        public void UpdateLaboratory_WithValidOwner_UpdatesAndReturnsLaboratory()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "lab123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Old Name",
                Description = "Old Description",
                Website = "http://old.com",
                ContactInfo = "old@lab.com",
                Creator = professor
            };

            var updateInfo = new LaboratoryUpdateDto
            {
                Name = "New Name",
                Description = "New Description",
                Website = "http://new.com",
                ContactInfo = "new@lab.com"
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);
            _mockLabRepository.Setup(r => r.Update(It.IsAny<Laboratory>())).Verifiable();

            // Act
            var result = _handler.UpdateLaboratory(professorId, labId, updateInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateInfo.Name, result.Name);
            Assert.Equal(updateInfo.Description, result.Description);
            Assert.Equal(updateInfo.Website, result.Website);
            Assert.Equal(updateInfo.ContactInfo, result.ContactInfo);

            _mockLabRepository.Verify(r => r.Update(It.IsAny<Laboratory>()), Times.Once);
        }

        [Fact]
        public void UpdateLaboratory_WithNullFields_OnlyUpdatesProvidedFields()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "lab123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Old Name",
                Description = "Old Description",
                Website = "http://old.com",
                ContactInfo = "old@lab.com",
                Creator = professor
            };

            var updateInfo = new LaboratoryUpdateDto
            {
                Name = "New Name",
                Description = null,
                Website = null,
                ContactInfo = "new@lab.com"
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);

            // Act
            var result = _handler.UpdateLaboratory(professorId, labId, updateInfo);

            // Assert
            Assert.Equal("New Name", result.Name);
            Assert.Equal("Old Description", result.Description); // Should keep old value
            Assert.Equal("http://old.com", result.Website);      // Should keep old value
            Assert.Equal("new@lab.com", result.ContactInfo);
        }

        [Fact]
        public void UpdateLaboratory_WithNonOwner_ThrowsException()
        {
            // Arrange
            var professorId = "prof123";
            var otherProfessorId = "prof456";
            var labId = "lab123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var otherProfessor = new Professor
            {
                UserID = otherProfessorId,
                Username = "otherProf"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Lab Name",
                Creator = professor
            };

            var updateInfo = new LaboratoryUpdateDto
            {
                Name = "New Name"
            };

            _mockUserRepository.Setup(r => r.GetUserById(otherProfessorId)).Returns(otherProfessor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.UpdateLaboratory(otherProfessorId, labId, updateInfo)
            );

            Assert.Equal("無權限更新此實驗室", exception.Message);
            _mockLabRepository.Verify(r => r.Update(It.IsAny<Laboratory>()), Times.Never);
        }

        [Fact]
        public void UpdateLaboratory_WithNonProfessorUser_ThrowsException()
        {
            // Arrange
            var userId = "student123";
            var labId = "lab123";

            var student = new Student
            {
                UserID = userId,
                Username = "testStudent"
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(student);

            var updateInfo = new LaboratoryUpdateDto
            {
                Name = "New Name"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.UpdateLaboratory(userId, labId, updateInfo)
            );

            Assert.Equal("無權限更新此實驗室", exception.Message);
        }

        [Fact]
        public void UpdateLaboratory_WithNonexistentLab_ThrowsException()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "nonexistent";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            var updateInfo = new LaboratoryUpdateDto
            {
                Name = "New Name"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.UpdateLaboratory(professorId, labId, updateInfo)
            );

            Assert.Equal("無權限更新此實驗室", exception.Message);
        }

        #endregion

        #region AddMember Tests

        [Fact]
        public void AddMember_WithValidProfessorAndLab_AddsAndReturnsStudent()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "lab123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = professor,
                Members = new List<User>()
            };

            var memberInfo = new MemberCreateDto
            {
                Username = "newStudent",
                Email = "student@test.com"
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);
            _mockLabRepository.Setup(r => r.Update(It.IsAny<Laboratory>())).Verifiable();
            _mockUserRepository.Setup(r => r.Add(It.IsAny<Student>())).Verifiable();
            _mockNotificationService.Setup(r => r.NotifyNewMember(It.IsAny<Student>(), It.IsAny<object>())).Verifiable();

            // Act
            var result = _handler.AddMember(professorId, labId, memberInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(memberInfo.Username, result.Username);
            Assert.Equal(memberInfo.Email, result.Email);
            Assert.Equal("Student", result.Role);
            Assert.NotNull(result.Password);

            _mockLabRepository.Verify(r => r.Update(It.IsAny<Laboratory>()), Times.Once);
            _mockUserRepository.Verify(r => r.Add(It.IsAny<Student>()), Times.Once);
            _mockNotificationService.Verify(r => r.NotifyNewMember(It.IsAny<Student>(), It.IsAny<object>()), Times.Once);

            Assert.Contains(result, laboratory.Members);
        }

        [Fact]
        public void AddMember_WithNonOwnerProfessor_ThrowsException()
        {
            // Arrange
            var professorId = "prof123";
            var otherProfessorId = "prof456";
            var labId = "lab123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var otherProfessor = new Professor
            {
                UserID = otherProfessorId,
                Username = "otherProf"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = professor
            };

            var memberInfo = new MemberCreateDto
            {
                Username = "newStudent",
                Email = "student@test.com"
            };

            _mockUserRepository.Setup(r => r.GetUserById(otherProfessorId)).Returns(otherProfessor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.AddMember(otherProfessorId, labId, memberInfo)
            );

            Assert.Equal("無權限添加成員到此實驗室", exception.Message);
            _mockLabRepository.Verify(r => r.Update(It.IsAny<Laboratory>()), Times.Never);
            _mockUserRepository.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        }

        [Fact]
        public void AddMember_WithNonProfessorUser_ThrowsException()
        {
            // Arrange
            var userId = "student123";
            var labId = "lab123";

            var student = new Student
            {
                UserID = userId,
                Username = "testStudent"
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(student);

            var memberInfo = new MemberCreateDto
            {
                Username = "newStudent",
                Email = "newstudent@test.com"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.AddMember(userId, labId, memberInfo)
            );

            Assert.Equal("無權限添加成員到此實驗室", exception.Message);
        }

        [Fact]
        public void AddMember_WithNonexistentLab_ThrowsException()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "nonexistent";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            var memberInfo = new MemberCreateDto
            {
                Username = "newStudent",
                Email = "student@test.com"
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.AddMember(professorId, labId, memberInfo)
            );

            Assert.Equal("無權限添加成員到此實驗室", exception.Message);
        }

        #endregion

        #region RemoveMember Tests

        [Fact]
        public void RemoveMember_WithValidProfessorAndExistingMember_RemovesAndReturnsTrue()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "lab123";
            var memberId = "student123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var student = new Student
            {
                UserID = memberId,
                Username = "testStudent"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = professor,
                Members = new List<User> { student }
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);
            _mockLabRepository.Setup(r => r.Update(It.IsAny<Laboratory>())).Verifiable();

            // Act
            var result = _handler.RemoveMember(professorId, labId, memberId);

            // Assert
            Assert.True(result);
            _mockLabRepository.Verify(r => r.Update(It.IsAny<Laboratory>()), Times.Once);
            Assert.DoesNotContain(student, laboratory.Members);
        }

        [Fact]
        public void RemoveMember_WithNonexistentMember_ReturnsFalse()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "lab123";
            var memberId = "nonexistent";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = professor,
                Members = new List<User>()
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);

            // Act
            var result = _handler.RemoveMember(professorId, labId, memberId);

            // Assert
            Assert.False(result);
            _mockLabRepository.Verify(r => r.Update(It.IsAny<Laboratory>()), Times.Never);
        }

        [Fact]
        public void RemoveMember_WithNonOwnerProfessor_ThrowsException()
        {
            // Arrange
            var professorId = "prof123";
            var otherProfessorId = "prof456";
            var labId = "lab123";
            var memberId = "student123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            var otherProfessor = new Professor
            {
                UserID = otherProfessorId,
                Username = "otherProf"
            };

            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab",
                Creator = professor
            };

            _mockUserRepository.Setup(r => r.GetUserById(otherProfessorId)).Returns(otherProfessor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.RemoveMember(otherProfessorId, labId, memberId)
            );

            Assert.Equal("無權限從此實驗室刪除成員", exception.Message);
        }

        [Fact]
        public void RemoveMember_WithNonProfessorUser_ThrowsException()
        {
            // Arrange
            var userId = "student123";
            var labId = "lab123";
            var memberId = "student456";

            var student = new Student
            {
                UserID = userId,
                Username = "testStudent"
            };

            _mockUserRepository.Setup(r => r.GetUserById(userId)).Returns(student);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.RemoveMember(userId, labId, memberId)
            );

            Assert.Equal("無權限從此實驗室刪除成員", exception.Message);
        }

        [Fact]
        public void RemoveMember_WithNonexistentLab_ThrowsException()
        {
            // Arrange
            var professorId = "prof123";
            var labId = "nonexistent";
            var memberId = "student123";

            var professor = new Professor
            {
                UserID = professorId,
                Username = "testProf"
            };

            _mockUserRepository.Setup(r => r.GetUserById(professorId)).Returns(professor);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => _handler.RemoveMember(professorId, labId, memberId)
            );

            Assert.Equal("無權限從此實驗室刪除成員", exception.Message);
        }

        #endregion

        #region GetLaboratoryDetails Tests

        [Fact]
        public void GetLaboratoryDetails_WithValidId_ReturnsLaboratory()
        {
            // Arrange
            var labId = "lab123";
            var laboratory = new Laboratory
            {
                LabID = labId,
                Name = "Test Lab"
            };

            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(laboratory);

            // Act
            var result = _handler.GetLaboratoryDetails(labId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(laboratory, result);
        }

        [Fact]
        public void GetLaboratoryDetails_WithNonexistentId_ReturnsNull()
        {
            // Arrange
            var labId = "nonexistent";
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            // Act
            var result = _handler.GetLaboratoryDetails(labId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetProfessorLaboratories Tests

        [Fact]
        public void GetProfessorLaboratories_ReturnsCorrectLaboratories()
        {
            // Arrange
            var professorId = "prof123";
            var labs = new List<Laboratory>
            {
                new Laboratory { LabID = "lab1", Name = "Lab 1" },
                new Laboratory { LabID = "lab2", Name = "Lab 2" }
            };

            _mockLabRepository.Setup(r => r.GetByCreator(professorId)).Returns(labs);

            // Act
            var result = _handler.GetProfessorLaboratories(professorId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(labs, result);
        }

        [Fact]
        public void GetProfessorLaboratories_WithNoLabs_ReturnsEmptyCollection()
        {
            // Arrange
            var professorId = "prof123";
            var emptyList = new List<Laboratory>();

            _mockLabRepository.Setup(r => r.GetByCreator(professorId)).Returns(emptyList);

            // Act
            var result = _handler.GetProfessorLaboratories(professorId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}