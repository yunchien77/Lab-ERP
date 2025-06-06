using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemoryFinanceRepository : IFinanceRepository
    {
        private static List<FinanceRecord> _records = new List<FinanceRecord>();
        private static int _nextId = 1;

        public List<FinanceRecord> GetByLaboratoryId(string laboratoryId)
        {
            return _records.Where(r => r.LaboratoryId == laboratoryId)
                          .OrderByDescending(r => r.CreatedAt)
                          .ToList();
        }

        public FinanceRecord GetById(int id)
        {
            return _records.FirstOrDefault(r => r.Id == id);
        }

        public void Add(FinanceRecord record)
        {
            record.Id = _nextId++;
            record.CreatedAt = DateTime.Now;
            _records.Add(record);
        }

        public void Update(FinanceRecord record)
        {
            var existing = GetById(record.Id);
            if (existing != null)
            {
                existing.Type = record.Type;
                existing.Amount = record.Amount;
                existing.Description = record.Description;
                existing.Category = record.Category;
                existing.RecordDate = record.RecordDate;
            }
        }

        public void Delete(int id)
        {
            var record = GetById(id);
            if (record != null)
            {
                _records.Remove(record);
            }
        }

        public decimal GetTotalIncome(string laboratoryId)
        {
            return _records.Where(r => r.LaboratoryId == laboratoryId && r.Type == "Income")
                          .Sum(r => r.Amount);
        }

        public decimal GetTotalExpense(string laboratoryId)
        {
            return _records.Where(r => r.LaboratoryId == laboratoryId && r.Type == "Expense")
                          .Sum(r => r.Amount);
        }

        public decimal GetBalance(string laboratoryId)
        {
            return GetTotalIncome(laboratoryId) - GetTotalExpense(laboratoryId);
        }
    }
}

