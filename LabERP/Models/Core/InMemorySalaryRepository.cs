using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemorySalaryRepository : ISalaryRepository
    {
        private static List<Salary> _salaries = new List<Salary>();
        private static int _nextId = 1;

        public List<Salary> GetByLaboratoryId(string laboratoryId)
        {
            return _salaries.Where(s => s.LaboratoryId == laboratoryId)
                           .OrderByDescending(s => s.CreatedAt)
                           .ToList();
        }

        public Salary GetById(int id)
        {
            return _salaries.FirstOrDefault(s => s.Id == id);
        }

        public Salary GetByUserAndLaboratory(string userId, string laboratoryId)
        {
            return _salaries.FirstOrDefault(s => s.UserId == userId && s.LaboratoryId == laboratoryId);
        }

        public void Add(Salary salary)
        {
            salary.Id = _nextId++;
            salary.CreatedAt = DateTime.Now;
            _salaries.Add(salary);
        }

        public void Update(Salary salary)
        {
            var existing = GetById(salary.Id);
            if (existing != null)
            {
                existing.Amount = salary.Amount;
                existing.EffectiveDate = salary.EffectiveDate;
                existing.Status = salary.Status;
                existing.UpdatedAt = DateTime.Now;

                // 重新計算次月5日發放日期
                var nextMonth = salary.EffectiveDate.AddMonths(1);
                existing.PaymentDate = new DateTime(nextMonth.Year, nextMonth.Month, 5);
            }
        }

        public void Delete(int id)
        {
            var salary = GetById(id);
            if (salary != null)
            {
                _salaries.Remove(salary);
            }
        }

        public List<Salary> GetPendingSalaries(string laboratoryId)
        {
            return _salaries.Where(s => s.LaboratoryId == laboratoryId && s.Status == "Pending")
                           .ToList();
        }
    }
}
