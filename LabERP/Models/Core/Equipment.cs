using System.ComponentModel.DataAnnotations;

namespace LabERP.Models.Core
{
    public class Equipment
    {
        [Key]
        public string EquipmentID { get; set; }

        [Required]
        [Display(Name = "設備名稱")]
        public string Name { get; set; }

        [Display(Name = "描述")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "總數量")]
        public int TotalQuantity { get; set; }

        [Display(Name = "可用數量")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "購買日期")]
        public DateTime PurchaseDate { get; set; }

        [Display(Name = "狀態")]
        public string Status { get; set; } = "Available"; // 預設為可用

        [Required]
        public string LaboratoryID { get; set; } // 所屬實驗室 ID

        public Laboratory Laboratory { get; set; }

        public List<BorrowRecord> BorrowRecords { get; set; } // 借用記錄

        // 建構子
        public Equipment()
        {
            EquipmentID = Guid.NewGuid().ToString();
            BorrowRecords = new List<BorrowRecord>();
        }

        // 添加借用記錄
        public bool AddBorrowRecord(BorrowRecord record)
        {
            if (record == null || record.Quantity > AvailableQuantity)
                return false;

            BorrowRecords.Add(record);
            AvailableQuantity -= record.Quantity;

            if (AvailableQuantity == 0)
                Status = "Unavailable";

            return true;
        }

        // 更新借用記錄（例如歸還設備）
        public bool UpdateBorrowRecord(string recordID, int returnQuantity)
        {
            var record = BorrowRecords.FirstOrDefault(r => r.RecordID == recordID && r.Status == "Borrowed");
            if (record == null || returnQuantity <= 0)
                return false;

            record.Status = "Returned";
            record.ReturnDate = DateTime.Now;
            AvailableQuantity += returnQuantity;

            if (AvailableQuantity > 0)
                Status = "Available";

            return true;
        }

        // 獲取所有借用記錄
        public List<BorrowRecord> GetAllBorrowRecords()
        {
            return BorrowRecords;
        }

        // 查找特定借用記錄
        public BorrowRecord FindBorrowRecord(string recordID)
        {
            return BorrowRecords.FirstOrDefault(r => r.RecordID == recordID);
        }
    }
}
