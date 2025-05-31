namespace LabERP.Models.Core
{
    public class Student : User
    {
        public string StudentID { get; set; }
        public string PhoneNumber { get; set; }
        public List<BorrowRecord> BorrowRecords { get; set; }

        public Student()
        {
            Role = "Student";
            BorrowRecords = new List<BorrowRecord>();
        }
    }
}