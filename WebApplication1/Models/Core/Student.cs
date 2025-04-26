namespace WebApplication1.Models.Core
{
    public class Student : User
    {
        public List<BorrowRecord> BorrowRecords { get; set; }

        public Student()
        {
            Role = "Student";
            BorrowRecords = new List<BorrowRecord>();
        }

        // 這裡應該有其他 Student 特有的方法，但目前為了登入功能我們只需要基本資訊
    }

    // 為了讓程式能編譯，加入一個簡單的 BorrowRecord 類別
    public class BorrowRecord
    {
        public string RecordID { get; set; }
        // 其他屬性...
    }
}
