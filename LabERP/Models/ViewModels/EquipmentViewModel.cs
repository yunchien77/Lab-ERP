using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class CreateEquipmentViewModel
    {
        [Required(ErrorMessage = "請輸入設備名稱")]
        [Display(Name = "設備名稱")]
        public string Name { get; set; }

        [Display(Name = "設備描述")]
        public string Description { get; set; }

        [Required(ErrorMessage = "請輸入數量")]
        [Range(1, int.MaxValue, ErrorMessage = "數量必須大於0")]
        [Display(Name = "數量")]
        public int TotalQuantity { get; set; }

        [Display(Name = "購入日期")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; }

        [Required(ErrorMessage = "請輸入實驗室ID")]
        public string LaboratoryID { get; set; }
    }

    public class BorrowEquipmentViewModel
    {
        [Required(ErrorMessage = "請輸入設備ID")]
        public string EquipmentID { get; set; }

        [Required(ErrorMessage = "請輸入借用數量")]
        [Range(1, int.MaxValue, ErrorMessage = "借用數量必須大於0")]
        [Display(Name = "借用數量")]
        public int Quantity { get; set; }

        [Display(Name = "備註")]
        public string Notes { get; set; }
    }

    public class ReturnEquipmentViewModel
    {
        [Required(ErrorMessage = "請輸入借用記錄ID")]
        public string RecordID { get; set; }

        [Display(Name = "備註")]
        public string Notes { get; set; }
    }
}