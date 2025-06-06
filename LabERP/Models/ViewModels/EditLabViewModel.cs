using System.ComponentModel.DataAnnotations;

namespace LabERP.Models.ViewModels
{
    public class EditLabViewModel
    {
        [Required(ErrorMessage = "請輸入實驗室ID")]
        public string LabID { get; set; }

        [Required(ErrorMessage = "請輸入實驗室名稱")]
        [Display(Name = "實驗室名稱")]
        public string Name { get; set; }

        [Display(Name = "描述")]
        public string Description { get; set; }

        [Display(Name = "網站")]
        public string Website { get; set; }

        [Display(Name = "聯絡資訊")]
        public string ContactInfo { get; set; }
    }
}