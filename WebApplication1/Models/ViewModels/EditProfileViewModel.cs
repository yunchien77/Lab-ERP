using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "請輸入學號")]
        [Display(Name = "學號")]
        public string StudentID { get; set; }

        [Required(ErrorMessage = "請輸入手機號碼")]
        [Display(Name = "手機號碼")]
        [Phone(ErrorMessage = "請輸入有效的手機號碼")]
        public string PhoneNumber { get; set; }
    }
}