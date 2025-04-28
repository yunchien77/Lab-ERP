// Models/Core/Student.cs
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Core
{
    public class Student : User
    {
        // Add new attributes
        public string StudentID { get; set; }
        public string PhoneNumber { get; set; }
        public List<BorrowRecord> BorrowRecords { get; set; }

        public Student()
        {
            Role = "Student";
            BorrowRecords = new List<BorrowRecord>();
        }
    }

    // Keep the BorrowRecord class as is
    public class BorrowRecord
    {
        [Key]
        public string RecordID { get; set; }

        public string StudentID { get; set; }
        public Student Student { get; set; }

        public string EquipmentID { get; set; }
        public Equipment Equipment { get; set; }

        [Required]
        [Display(Name = "借用日期")]
        public DateTime BorrowDate { get; set; }

        [Display(Name = "歸還日期")]
        public DateTime? ReturnDate { get; set; }

        [Display(Name = "借用數量")]
        public int Quantity { get; set; }

        [Display(Name = "借用狀態")]
        public string Status { get; set; } // "Borrowed", "Returned"

        [Display(Name = "備註")]
        public string Notes { get; set; }

        // Constructor
        public BorrowRecord()
        {
            RecordID = Guid.NewGuid().ToString();
            BorrowDate = DateTime.Now;
            Status = "Borrowed";
        }
    }
}