namespace LabERP.Models.Core
{
    public enum ExpenseRequestStatus
    {
        Pending,    // 未審核
        Approved,   // 已通過
        Rejected    // 不通過
    }

    public class ExpenseRequest
    {
        public int Id { get; set; }
        public string LaboratoryId { get; set; }
        public string RequesterId { get; set; }  // 申請人ID
        public string RequesterName { get; set; } // 申請人姓名
        public decimal Amount { get; set; }
        public string InvoiceNumber { get; set; }
        public string Category { get; set; }  // 設備採購、材料費、差旅費、其他
        public string Description { get; set; }
        public string Purpose { get; set; }  // 用途說明
        public ExpenseRequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string ReviewedBy { get; set; }  // 審核者ID
        public string ReviewNotes { get; set; } // 審核備註
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 導航屬性
        public List<ExpenseAttachment> Attachments { get; set; }

        public ExpenseRequest()
        {
            Status = ExpenseRequestStatus.Pending;
            RequestDate = DateTime.Now;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            Attachments = new List<ExpenseAttachment>();
        }
    }

    public class ExpenseAttachment
    {
        public int Id { get; set; }
        public int ExpenseRequestId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }

        public ExpenseAttachment()
        {
            UploadedAt = DateTime.Now;
        }
    }
}
