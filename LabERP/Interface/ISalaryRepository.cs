using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface ISalaryRepository
    {
        List<Salary> GetByLaboratoryId(string laboratoryId);
        Salary GetById(int id);
        Salary GetByUserAndLaboratory(string userId, string laboratoryId);
        void Add(Salary salary);
        void Update(Salary salary);
        void Delete(int id);
        List<Salary> GetPendingSalaries(string laboratoryId);
    }
}
