using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemoryBankAccountRepository : IBankAccountRepository
    {
        private static List<BankAccount> _accounts = new List<BankAccount>();
        private static int _nextId = 1;

        public BankAccount GetByLaboratoryId(string laboratoryId)
        {
            return _accounts.FirstOrDefault(a => a.LaboratoryId == laboratoryId && a.IsActive);
        }

        public void Add(BankAccount account)
        {
            account.Id = _nextId++;
            account.CreatedAt = DateTime.Now;
            _accounts.Add(account);
        }

        public void Update(BankAccount account)
        {
            var existing = _accounts.FirstOrDefault(a => a.Id == account.Id);
            if (existing != null)
            {
                existing.BankName = account.BankName;
                existing.AccountNumber = account.AccountNumber;
                existing.AccountHolder = account.AccountHolder;
                existing.BranchCode = account.BranchCode;
                existing.UpdatedAt = DateTime.Now;
            }
        }

        public void Delete(int id)
        {
            var account = _accounts.FirstOrDefault(a => a.Id == id);
            if (account != null)
            {
                account.IsActive = false;
                account.UpdatedAt = DateTime.Now;
            }
        }
    }
}
