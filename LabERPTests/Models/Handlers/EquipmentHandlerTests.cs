using Xunit;
using LabERP.Models.Handlers;
using LabERP.Interface;
using LabERP.Models.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Moq.Protected;

namespace LabERP.Tests.Handlers
{
    public class EquipmentHandlerTests
    {
        private readonly Mock<ILaboratoryRepository> _mockLabRepository;
        private readonly Mock<IEquipmentRepository> _mockEquipRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly EquipmentHandler _equipmentHandler;

        public EquipmentHandlerTests()
        {
            _mockLabRepository = new Mock<ILaboratoryRepository>();
            _mockEquipRepository = new Mock<IEquipmentRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _equipmentHandler = new EquipmentHandler(
                _mockLabRepository.Object,
                _mockEquipRepository.Object,
                _mockUserRepository.Object);
        }

        #region CreateEquipment Tests
        [Fact]
        public void CreateEquipment_ValidProfessorAndLabInfo_CreatesEquipment()
        {
            // Arrange
            string professorId = "prof123";
            string labId = "lab123";
            var professor = new Professor { UserID = professorId };
            var lab = new Laboratory { LabID = labId, Creator = professor };
            var equipmentInfo = new CreateEquipmentDto
            {
                Name = "Test Equipment",
                Description = "For testing",
                TotalQuantity = 10,
                PurchaseDate = DateTime.Now,
                LaboratoryID = labId
            };

            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act
            var result = _equipmentHandler.CreateEquipment(professorId, equipmentInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(equipmentInfo.Name, result.Name);
            Assert.Equal(equipmentInfo.TotalQuantity, result.TotalQuantity);
            Assert.Equal(equipmentInfo.TotalQuantity, result.AvailableQuantity);
            _mockEquipRepository.Verify(r => r.Add(It.IsAny<Equipment>()), Times.Once);
        }

        [Fact]
        public void CreateEquipment_LabDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string professorId = "prof123";
            string labId = "nonExistentLab";
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns((Laboratory)null);

            var equipmentInfo = new CreateEquipmentDto
            {
                Name = "Test Equipment",
                LaboratoryID = labId
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.CreateEquipment(professorId, equipmentInfo));
            Assert.Equal("實驗室不存在", exception.Message);
        }

        [Fact]
        public void CreateEquipment_ProfessorNotLabCreator_ThrowsInvalidOperationException()
        {
            // Arrange
            string professorId = "prof123";
            string labId = "lab123";
            var differentProfessor = new Professor { UserID = "differentProf" };
            var lab = new Laboratory { LabID = labId, Creator = differentProfessor };
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            var equipmentInfo = new CreateEquipmentDto
            {
                Name = "Test Equipment",
                LaboratoryID = labId
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.CreateEquipment(professorId, equipmentInfo));
            Assert.Equal("無權限新增設備到此實驗室", exception.Message);
        }
        #endregion

        #region DeleteEquipment Tests
        [Fact]
        public void DeleteEquipment_ValidProfessorAndEquipment_DeletesEquipment()
        {
            // Arrange
            string professorId = "prof123";
            string equipmentId = "equip123";
            string labId = "lab123";

            var professor = new Professor { UserID = professorId };
            var lab = new Laboratory { LabID = labId, Creator = professor };
            var equipment = new Equipment { EquipmentID = equipmentId, LaboratoryID = labId };

            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act
            _equipmentHandler.DeleteEquipment(professorId, equipmentId);

            // Assert
            _mockEquipRepository.Verify(r => r.Delete(equipmentId), Times.Once);
        }

        [Fact]
        public void DeleteEquipment_EquipmentDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string professorId = "prof123";
            string equipmentId = "nonExistentEquip";

            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns((Equipment)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.DeleteEquipment(professorId, equipmentId));
            Assert.Equal("設備不存在", exception.Message);
        }

        [Fact]
        public void DeleteEquipment_ProfessorNotLabCreator_ThrowsInvalidOperationException()
        {
            // Arrange
            string professorId = "prof123";
            string equipmentId = "equip123";
            string labId = "lab123";

            var differentProfessor = new Professor { UserID = "differentProf" };
            var lab = new Laboratory { LabID = labId, Creator = differentProfessor };
            var equipment = new Equipment { EquipmentID = equipmentId, LaboratoryID = labId };

            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.DeleteEquipment(professorId, equipmentId));
            Assert.Equal("無權限刪除此設備", exception.Message);
        }

        [Fact]
        public void DeleteEquipment_WithActiveBorrowRecords_ThrowsInvalidOperationException()
        {
            // Arrange
            string professorId = "prof123";
            string equipmentId = "equip123";
            string labId = "lab123";

            var professor = new Professor { UserID = professorId };
            var lab = new Laboratory { LabID = labId, Creator = professor };
            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                LaboratoryID = labId,
                BorrowRecords = new List<BorrowRecord>
                {
                    new BorrowRecord { Status = "Borrowed" }
                }
            };

            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.DeleteEquipment(professorId, equipmentId));
            Assert.Equal("設備有未歸還的借用記錄，無法刪除", exception.Message);
        }
        #endregion

        #region BorrowEquipment Tests
        [Fact]
        public void BorrowEquipment_ValidStudentAndEquipment_CreatesBorrowRecord()
        {
            // Arrange
            string studentId = "student123";
            string equipmentId = "equip123";

            var student = new Student { UserID = studentId, BorrowRecords = new List<BorrowRecord>() };
            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                AvailableQuantity = 5,
                BorrowRecords = new List<BorrowRecord>()
            };

            var borrowInfo = new BorrowEquipmentDto
            {
                EquipmentID = equipmentId,
                Quantity = 2,
                Notes = "Test borrow"
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);

            // Act
            var result = _equipmentHandler.BorrowEquipment(studentId, borrowInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(studentId, result.StudentID);
            Assert.Equal(equipmentId, result.EquipmentID);
            Assert.Equal(borrowInfo.Quantity, result.Quantity);
            Assert.Equal("Borrowed", result.Status);
            _mockEquipRepository.Verify(r => r.Update(It.IsAny<Equipment>()), Times.Once);
            Assert.Single(student.BorrowRecords);
        }

        [Fact]
        public void BorrowEquipment_EquipmentDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string equipmentId = "nonExistentEquip";

            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns((Equipment)null);

            var borrowInfo = new BorrowEquipmentDto
            {
                EquipmentID = equipmentId,
                Quantity = 1
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.BorrowEquipment(studentId, borrowInfo));
            Assert.Equal("設備不可用或借用數量超過可用數量", exception.Message);
        }

        [Fact]
        public void BorrowEquipment_InsufficientQuantity_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string equipmentId = "equip123";

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                AvailableQuantity = 1
            };

            var borrowInfo = new BorrowEquipmentDto
            {
                EquipmentID = equipmentId,
                Quantity = 2
            };

            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.BorrowEquipment(studentId, borrowInfo));
            Assert.Equal("設備不可用或借用數量超過可用數量", exception.Message);
        }

        [Fact]
        public void BorrowEquipment_StudentDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "nonExistentStudent";
            string equipmentId = "equip123";

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                AvailableQuantity = 5
            };

            var borrowInfo = new BorrowEquipmentDto
            {
                EquipmentID = equipmentId,
                Quantity = 1
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns((User)null);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.BorrowEquipment(studentId, borrowInfo));
            Assert.Equal("學生不存在", exception.Message);
        }

        [Fact]
        public void BorrowEquipment_FailsToAddBorrowRecord_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string equipmentId = "equip123";

            var student = new Student { UserID = studentId, BorrowRecords = new List<BorrowRecord>() };
            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                AvailableQuantity = 1 // 可用數量不足
            };

            var borrowInfo = new BorrowEquipmentDto
            {
                EquipmentID = equipmentId,
                Quantity = 2 // 借用數量超過可用數量
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.BorrowEquipment(studentId, borrowInfo));
            Assert.Equal("設備不可用或借用數量超過可用數量", exception.Message);
        }
        #endregion

        #region ReturnEquipment Tests
        [Fact]
        public void ReturnEquipment_ValidStudentAndRecord_UpdatesBorrowRecord()
        {
            // Arrange
            string studentId = "student123";
            string recordId = "record123";
            string equipmentId = "equip123";

            var borrowRecord = new BorrowRecord
            {
                RecordID = recordId,
                EquipmentID = equipmentId,
                Status = "Borrowed",
                Quantity = 1
            };

            var student = new Student
            {
                UserID = studentId,
                BorrowRecords = new List<BorrowRecord> { borrowRecord }
            };

            var equipment = new Equipment { EquipmentID = equipmentId };
            equipment.BorrowRecords = new List<BorrowRecord> { borrowRecord };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);

            // Act
            _equipmentHandler.ReturnEquipment(studentId, recordId);

            // Assert
            _mockEquipRepository.Verify(r => r.Update(It.IsAny<Equipment>()), Times.Once);
        }

        [Fact]
        public void ReturnEquipment_StudentDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "nonExistentStudent";
            string recordId = "record123";

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns((User)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.ReturnEquipment(studentId, recordId));
            Assert.Equal("學生不存在", exception.Message);
        }

        [Fact]
        public void ReturnEquipment_RecordDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string recordId = "nonExistentRecord";

            var student = new Student
            {
                UserID = studentId,
                BorrowRecords = new List<BorrowRecord>()
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.ReturnEquipment(studentId, recordId));
            Assert.Equal("借用記錄不存在或已歸還", exception.Message);
        }

        [Fact]
        public void ReturnEquipment_EquipmentDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string recordId = "record123";
            string equipmentId = "nonExistentEquip";

            var borrowRecord = new BorrowRecord
            {
                RecordID = recordId,
                EquipmentID = equipmentId,
                Status = "Borrowed"
            };

            var student = new Student
            {
                UserID = studentId,
                BorrowRecords = new List<BorrowRecord> { borrowRecord }
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns((Equipment)null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.ReturnEquipment(studentId, recordId));
            Assert.Equal("設備不存在", exception.Message);
        }

        [Fact]
        public void ReturnEquipment_FailsToUpdateBorrowRecord_ThrowsInvalidOperationException()
        {
            // Arrange
            string studentId = "student123";
            string recordId = "record123";
            string equipmentId = "equip123";

            var borrowRecord = new BorrowRecord
            {
                RecordID = recordId,
                EquipmentID = equipmentId,
                Status = "Returned", // 狀態不是 "Borrowed"
                Quantity = 1
            };

            var student = new Student
            {
                UserID = studentId,
                BorrowRecords = new List<BorrowRecord> { borrowRecord }
            };

            var equipment = new Equipment
            {
                EquipmentID = equipmentId,
                BorrowRecords = new List<BorrowRecord> { borrowRecord }
            };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _equipmentHandler.ReturnEquipment(studentId, recordId));
            Assert.Equal("借用記錄不存在或已歸還", exception.Message);
        }

        #endregion

        #region GetEquipmentsByLab Tests
        [Fact]
        public void GetEquipmentsByLab_ValidLabId_ReturnsEquipments()
        {
            // Arrange
            string labId = "lab123";
            var lab = new Laboratory { LabID = labId };
            var equipments = new List<Equipment>
            {
                new Equipment { EquipmentID = "equip1", LaboratoryID = labId },
                new Equipment { EquipmentID = "equip2", LaboratoryID = labId }
            };

            _mockEquipRepository.Setup(r => r.GetByLaboratoryId(labId)).Returns(equipments);
            _mockLabRepository.Setup(r => r.GetById(labId)).Returns(lab);

            // Act
            var result = _equipmentHandler.GetEquipmentsByLab(labId).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, equip => Assert.Equal(lab, equip.Laboratory));
        }

        [Fact]
        public void GetEquipmentsByLab_EmptyLabId_ReturnsEmptyList()
        {
            // Arrange
            string labId = "";

            // Act
            var result = _equipmentHandler.GetEquipmentsByLab(labId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetEquipmentsByLab_NullLabId_ReturnsEmptyList()
        {
            // Arrange
            string labId = null;

            // Act
            var result = _equipmentHandler.GetEquipmentsByLab(labId);

            // Assert
            Assert.Empty(result);
        }
        #endregion

        #region GetEquipmentById Tests
        [Fact]
        public void GetEquipmentById_ValidId_ReturnsEquipment()
        {
            // Arrange
            string equipmentId = "equip123";
            var equipment = new Equipment { EquipmentID = equipmentId };
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns(equipment);

            // Act
            var result = _equipmentHandler.GetEquipmentById(equipmentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(equipmentId, result.EquipmentID);
        }

        [Fact]
        public void GetEquipmentById_InvalidId_ReturnsNull()
        {
            // Arrange
            string equipmentId = "nonExistentEquip";
            _mockEquipRepository.Setup(r => r.GetById(equipmentId)).Returns((Equipment)null);

            // Act
            var result = _equipmentHandler.GetEquipmentById(equipmentId);

            // Assert
            Assert.Null(result);
        }
        #endregion

        #region GetStudentBorrowRecords Tests
        [Fact]
        public void GetStudentBorrowRecords_ValidStudent_ReturnsBorrowRecords()
        {
            // Arrange
            string studentId = "student123";
            string equipmentId1 = "equip1";
            string equipmentId2 = "equip2";

            var records = new List<BorrowRecord>
            {
                new BorrowRecord { RecordID = "record1", EquipmentID = equipmentId1 },
                new BorrowRecord { RecordID = "record2", EquipmentID = equipmentId2 }
            };

            var student = new Student { UserID = studentId, BorrowRecords = records };

            var equipment1 = new Equipment { EquipmentID = equipmentId1 };
            var equipment2 = new Equipment { EquipmentID = equipmentId2 };

            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId1)).Returns(equipment1);
            _mockEquipRepository.Setup(r => r.GetById(equipmentId2)).Returns(equipment2);

            // Act
            var result = _equipmentHandler.GetStudentBorrowRecords(studentId).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(equipment1, result[0].Equipment);
            Assert.Equal(equipment2, result[1].Equipment);
        }

        [Fact]
        public void GetStudentBorrowRecords_StudentDoesNotExist_ReturnsEmptyList()
        {
            // Arrange
            string studentId = "nonExistentStudent";
            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns((User)null);

            // Act
            var result = _equipmentHandler.GetStudentBorrowRecords(studentId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetStudentBorrowRecords_StudentHasNoBorrowRecords_ReturnsEmptyList()
        {
            // Arrange
            string studentId = "student123";
            var student = new Student { UserID = studentId, BorrowRecords = null };
            _mockUserRepository.Setup(r => r.GetUserById(studentId)).Returns(student);

            // Act
            var result = _equipmentHandler.GetStudentBorrowRecords(studentId);

            // Assert
            Assert.Empty(result);
        }
        #endregion
    }
}