using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IFinanceRepository
    {
        List<FinanceRecord> GetByLaboratoryId(string laboratoryId);
        FinanceRecord GetById(int id);
        void Add(FinanceRecord record);
        void Update(FinanceRecord record);
        void Delete(int id);
        decimal GetTotalIncome(string laboratoryId);
        decimal GetTotalExpense(string laboratoryId);
        decimal GetBalance(string laboratoryId);
    }
}
