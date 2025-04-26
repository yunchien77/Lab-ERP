// Models/Core/Student.cs
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
        public string RecordID { get; set; }
        // 其他屬性...
    }
}