using LabERP.Models.Core;
using System.ComponentModel.DataAnnotations;

namespace LabERP.Models.ViewModels
{
    public class ExpenseRequestListViewModel
    {
        public string LaboratoryId { get; set; }
        public string LaboratoryName { get; set; }
        public string CurrentUserId { get; set; }
        public string CurrentUserRole { get; set; }
        public bool IsProfessor { get; set; }
        public bool IsLabCreator { get; set; }
        public decimal AvailableBudget { get; set; }
        public List<ExpenseRequestItem> ExpenseRequests { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

        public ExpenseRequestListViewModel()
        {
            ExpenseRequests = new List<ExpenseRequestItem>();
        }
    }

    public class ExpenseRequestItem
    {
        public int Id { get; set; }
        public string RequesterName { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public ExpenseRequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string ReviewNotes { get; set; }
        public bool CanDelete { get; set; }
        public int AttachmentCount { get; set; }

        public string StatusText => Status switch
        {
            ExpenseRequestStatus.Pending => "未審核",
            ExpenseRequestStatus.Approved => "已通過",
            ExpenseRequestStatus.Rejected => "不通過",
            _ => "未知"
        };

        public string StatusBadgeClass => Status switch
        {
            ExpenseRequestStatus.Pending => "bg-warning",
            ExpenseRequestStatus.Approved => "bg-success",
            ExpenseRequestStatus.Rejected => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public class CreateExpenseRequestViewModel
    {
        public string LaboratoryId { get; set; }
        public string LaboratoryName { get; set; }

        [Required(ErrorMessage = "請輸入報帳金額")]
        [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "請輸入發票號碼")]
        [Display(Name = "發票號碼")]
        public string InvoiceNumber { get; set; }

        [Required(ErrorMessage = "請選擇報帳類別")]
        [Display(Name = "報帳類別")]
        public string Category { get; set; }

        [Required(ErrorMessage = "請輸入費用描述")]
        [Display(Name = "費用描述")]
        public string Description { get; set; }

        [Required(ErrorMessage = "請輸入用途說明")]
        [Display(Name = "用途說明")]
        public string Purpose { get; set; }

        [Display(Name = "發票/收據圖片")]
        public List<IFormFile> Attachments { get; set; }

        public List<string> CategoryOptions { get; set; }

        public CreateExpenseRequestViewModel()
        {
            CategoryOptions = new List<string> { "設備採購", "材料費", "差旅費", "其他" };
            Attachments = new List<IFormFile>();
        }
    }

    public class ExpenseRequestDetailViewModel
    {
        public ExpenseRequest ExpenseRequest { get; set; }
        public string LaboratoryName { get; set; }
        public bool CanDelete { get; set; }
        public bool CanReview { get; set; }
        public string CurrentUserId { get; set; }
        public decimal AvailableBudget { get; set; }
    }

    public class ReviewExpenseRequestViewModel
    {
        public int ExpenseRequestId { get; set; }
        public string LaboratoryId { get; set; }
        public string LaboratoryName { get; set; }
        public string RequesterName { get; set; }
        public decimal Amount { get; set; }
        public string InvoiceNumber { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Purpose { get; set; }
        public DateTime RequestDate { get; set; }
        public List<ExpenseAttachment> Attachments { get; set; }
        public decimal AvailableBudget { get; set; }
        public bool InsufficientBudget { get; set; }

        [Required(ErrorMessage = "請選擇審核結果")]
        public bool? Approved { get; set; }

        [Display(Name = "審核備註")]
        public string ReviewNotes { get; set; }

        public ReviewExpenseRequestViewModel()
        {
            Attachments = new List<ExpenseAttachment>();
        }
    }
}
