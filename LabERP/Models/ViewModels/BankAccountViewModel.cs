using System.ComponentModel.DataAnnotations;

namespace LabERP.Models.ViewModels
{
    public class BankAccountViewModel
    {
        public string LaboratoryId { get; set; }

        [Required(ErrorMessage = "請輸入銀行名稱")]
        [StringLength(50, ErrorMessage = "銀行名稱不能超過50字")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "請輸入帳戶號碼")]
        [StringLength(20, ErrorMessage = "帳戶號碼不能超過20字")]
        public string AccountNumber { get; set; }

        [Required(ErrorMessage = "請輸入戶名")]
        [StringLength(50, ErrorMessage = "戶名不能超過50字")]
        public string AccountHolder { get; set; }

        [StringLength(10, ErrorMessage = "分行代碼不能超過10字")]
        public string BranchCode { get; set; }
    }
}
