using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IBankAccountRepository
    {
        BankAccount GetByLaboratoryId(string laboratoryId);
        void Add(BankAccount account);
        void Update(BankAccount account);
        void Delete(int id);
    }
}
