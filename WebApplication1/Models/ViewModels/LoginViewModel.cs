using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "請輸入使用者名稱")]
        [Display(Name = "使用者名稱")]
        public string Username { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Display(Name = "記住我")]
        public bool RememberMe { get; set; }
    }
}
