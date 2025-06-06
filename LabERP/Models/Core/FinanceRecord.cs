namespace LabERP.Models.Core
{
    public class FinanceRecord
    {
        public int Id { get; set; }
        public string LaboratoryId { get; set; }
        public string Type { get; set; } // "Income" or "Expense"
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DateTime RecordDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } // Professor ID

        public FinanceRecord()
        {
            CreatedAt = DateTime.Now;
            RecordDate = DateTime.Now;
        }
    }
}
