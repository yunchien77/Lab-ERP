using System.ComponentModel.DataAnnotations;

namespace LabERP.Models.ViewModels
{
    public class AddMemberViewModel
    {
        [Required(ErrorMessage = "請輸入實驗室ID")]
        public string LabID { get; set; }

        [Required(ErrorMessage = "請輸入使用者名稱")]
        [Display(Name = "使用者名稱")]
        public string Username { get; set; }

        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子郵件")]
        [Display(Name = "電子郵件")]
        public string Email { get; set; }
    }
}
