using LabERP.Models.Core;
using System.Collections.Generic;

namespace LabERP.Interface
{
    public interface IEquipmentHandler
    {
        // 新增設備
        Equipment CreateEquipment(string professorId, CreateEquipmentDto equipmentInfo);

        // 刪除設備
        void DeleteEquipment(string professorId, string equipmentId);

        // 借用設備
        BorrowRecord BorrowEquipment(string studentId, BorrowEquipmentDto borrowInfo);

        // 歸還設備
        void ReturnEquipment(string studentId, string recordId);

        // 獲取實驗室的設備列表
        IEnumerable<Equipment> GetEquipmentsByLab(string labId);

        // 根據設備 ID 獲取設備
        Equipment GetEquipmentById(string equipmentId);

        // 獲取學生的借用記錄
        IEnumerable<BorrowRecord> GetStudentBorrowRecords(string studentId);
    }
}
