using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface ILaboratoryRepository
    {
        Laboratory GetById(string labId);
        void Add(Laboratory laboratory);
        void Update(Laboratory laboratory);
        IEnumerable<Laboratory> GetAll();
        IEnumerable<Laboratory> GetByCreator(string professorId);
    }
}
