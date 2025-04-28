using WebApplication1.Models.Core;

namespace WebApplication1.Models.Core
{
    public class BorrowRecord
    {
        public string RecordID { get; set; }
        public string StudentID { get; set; }
        public string EquipmentID { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        // 添加導航屬性
        public Equipment Equipment { get; set; }
        public Student Student { get; set; }

        // 其他方法保持不變
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(RecordID) &&
                   !string.IsNullOrEmpty(StudentID) &&
                   !string.IsNullOrEmpty(EquipmentID) &&
                   Quantity > 0 &&
                   !string.IsNullOrEmpty(Status) &&
                   BorrowDate != default;
        }
    }
}

