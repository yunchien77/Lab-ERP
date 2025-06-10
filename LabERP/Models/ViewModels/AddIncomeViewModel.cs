using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace LabERP.Models.ViewModels
{
    public class AddIncomeViewModel
    {
        public string LaboratoryId { get; set; } = "";

        public string LaboratoryName { get; set; } = "";

        [Required(ErrorMessage = "請輸入收入金額")]
        [Range(0.01, double.MaxValue, ErrorMessage = "金額必須大於0")]
        public decimal Amount { get; set; } = 0;

        [Required(ErrorMessage = "請輸入收入描述")]
        [StringLength(200, ErrorMessage = "描述不能超過200字")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "請選擇收入類別")]
        public string Category { get; set; } = "";

        [Required(ErrorMessage = "請選擇收入日期")]
        public DateTime RecordDate { get; set; }

        public AddIncomeViewModel()
        {
            RecordDate = DateTime.Now;
        }

    }
}
