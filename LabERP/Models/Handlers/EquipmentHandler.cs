using WebApplication1.Interface;
using WebApplication1.Models.Core;

namespace WebApplication1.Models.Handlers
{
    public class EquipmentHandler
    {
        private readonly ILaboratoryRepository _laboratoryRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IUserRepository _userRepository;

        public EquipmentHandler(
            ILaboratoryRepository laboratoryRepository,
            IEquipmentRepository equipmentRepository,
            IUserRepository userRepository)
        {
            _laboratoryRepository = laboratoryRepository;
            _equipmentRepository = equipmentRepository;
            _userRepository = userRepository;
        }

        // 新增設備
        public Equipment CreateEquipment(string professorId, CreateEquipmentDto equipmentInfo)
        {
            var lab = _laboratoryRepository.GetById(equipmentInfo.LaboratoryID);
            if (lab == null)
            {
                throw new InvalidOperationException("實驗室不存在");
            }

            if (lab.Creator?.UserID != professorId)
            {
                throw new InvalidOperationException("無權限新增設備到此實驗室");
            }

            var equipment = new Equipment
            {
                Name = equipmentInfo.Name,
                Description = equipmentInfo.Description,
                TotalQuantity = equipmentInfo.TotalQuantity,
                AvailableQuantity = equipmentInfo.TotalQuantity,
                PurchaseDate = equipmentInfo.PurchaseDate,
                LaboratoryID = equipmentInfo.LaboratoryID
            };

            _equipmentRepository.Add(equipment);
            return equipment;
        }

        // 刪除設備
        public void DeleteEquipment(string professorId, string equipmentId)
        {
            var equipment = _equipmentRepository.GetById(equipmentId);
            if (equipment == null)
            {
                throw new InvalidOperationException("設備不存在");
            }

            var lab = _laboratoryRepository.GetById(equipment.LaboratoryID);
            if (lab == null || lab.Creator.UserID != professorId)
            {
                throw new InvalidOperationException("無權限刪除此設備");
            }

            // 檢查是否有未歸還的借用記錄
            if (equipment.BorrowRecords.Any(r => r.Status == "Borrowed"))
            {
                throw new InvalidOperationException("設備有未歸還的借用記錄，無法刪除");
            }

            _equipmentRepository.Delete(equipmentId);
        }

        // 借用設備
        public BorrowRecord BorrowEquipment(string studentId, BorrowEquipmentDto borrowInfo)
        {
            var equipment = _equipmentRepository.GetById(borrowInfo.EquipmentID);
            if (equipment == null || equipment.AvailableQuantity < borrowInfo.Quantity)
            {
                throw new InvalidOperationException("設備不可用或借用數量超過可用數量");
            }

            var student = _userRepository.GetUserById(studentId) as Student;
            if (student == null)
            {
                throw new InvalidOperationException("學生不存在");
            }

            var record = new BorrowRecord
            {
                RecordID = Guid.NewGuid().ToString(),
                StudentID = student.UserID,
                EquipmentID = equipment.EquipmentID,
                Quantity = borrowInfo.Quantity,
                Notes = borrowInfo.Notes,
                BorrowDate = DateTime.Now,
                Status = "Borrowed"
            };

            if (!equipment.AddBorrowRecord(record))
            {
                throw new InvalidOperationException("無法添加借用記錄");
            }

            _equipmentRepository.Update(equipment);
            student.BorrowRecords.Add(record);
            return record;
        }

        // 歸還設備
        public void ReturnEquipment(string studentId, string recordId)
        {
            var student = _userRepository.GetUserById(studentId) as Student;
            if (student == null)
            {
                throw new InvalidOperationException("學生不存在");
            }

            var record = student.BorrowRecords.FirstOrDefault(r => r.RecordID == recordId && r.Status == "Borrowed");
            if (record == null)
            {
                throw new InvalidOperationException("借用記錄不存在或已歸還");
            }

            var equipment = _equipmentRepository.GetById(record.EquipmentID);
            if (equipment == null)
            {
                throw new InvalidOperationException("設備不存在");
            }

            if (!equipment.UpdateBorrowRecord(record.RecordID, record.Quantity))
            {
                throw new InvalidOperationException("無法更新借用記錄");
            }

            _equipmentRepository.Update(equipment);
        }

        // 獲取實驗室的設備列表
        public IEnumerable<Equipment> GetEquipmentsByLab(string labId)
        {
            if (string.IsNullOrEmpty(labId))
            {
                return Enumerable.Empty<Equipment>();
            }

            var equipments = _equipmentRepository.GetByLaboratoryId(labId);
            // 載入實驗室信息
            var lab = _laboratoryRepository.GetById(labId);
            foreach (var equipment in equipments)
            {
                equipment.Laboratory = lab;
            }

            return equipments;
        }

        public Equipment GetEquipmentById(string equipmentId)
        {
            return _equipmentRepository.GetById(equipmentId);
        }

        // 獲取學生的借用記錄
        public IEnumerable<BorrowRecord> GetStudentBorrowRecords(string studentId)
        {
            var student = _userRepository.GetUserById(studentId) as Student;
            if (student == null || student.BorrowRecords == null)
                return Enumerable.Empty<BorrowRecord>();

            // 填充每個記錄的設備信息
            foreach (var record in student.BorrowRecords)
            {
                record.Equipment = _equipmentRepository.GetById(record.EquipmentID);
            }

            return student.BorrowRecords;
        }
    }
}