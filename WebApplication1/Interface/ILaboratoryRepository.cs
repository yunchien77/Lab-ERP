using WebApplication1.Models.Core;

namespace WebApplication1.Interface
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
