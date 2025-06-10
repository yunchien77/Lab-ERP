namespace LabERP.Models.Core
{
    public class BankAccount
    {
        public int Id { get; set; }
        public string LaboratoryId { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
        public string BranchCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public BankAccount()
        {
            CreatedAt = DateTime.Now;
            IsActive = true;
        }
    }
}
