namespace LabERP.Models.Core
{
    public class Salary
    {
        public int Id { get; set; }
        public string LaboratoryId { get; set; }
        public string UserId { get; set; } // Student ID
        public string UserName { get; set; }
        public decimal Amount { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime PaymentDate { get; set; } // 次月5日
        public string Status { get; set; } // "Pending", "Paid"
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } // Professor ID

        public Salary()
        {
            CreatedAt = DateTime.Now;
            EffectiveDate = DateTime.Now;
            Status = "Pending";
            // 設定次月5日為發放日期
            var nextMonth = DateTime.Now.AddMonths(1);
            PaymentDate = new DateTime(nextMonth.Year, nextMonth.Month, 5);
        }
    }
}
