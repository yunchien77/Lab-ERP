using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Core
{
    public class Equipment
    {
        [Key]
        public string EquipmentID { get; set; }

        [Required]
        [Display(Name = "設備名稱")]
        public string Name { get; set; }

        [Display(Name = "設備描述")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "總數量")]
        public int TotalQuantity { get; set; }

        [Display(Name = "可用數量")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "購入日期")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; }

        [Display(Name = "設備狀態")]
        public string Status { get; set; } // Available, Maintenance, etc.

        public string LaboratoryID { get; set; }
        public Laboratory Laboratory { get; set; }

        public List<BorrowRecord> BorrowRecords { get; set; }

        // Constructor
        public Equipment()
        {
            EquipmentID = Guid.NewGuid().ToString();
            PurchaseDate = DateTime.Now;
            Status = "Available";
            BorrowRecords = new List<BorrowRecord>();
        }
    }
}