using WebApplication1.Models.Core;

namespace WebApplication1.Interface
{
    public interface IEquipmentRepository
    {
        Equipment GetById(string equipmentId);
        void Add(Equipment equipment);
        void Update(Equipment equipment);
        void Delete(string equipmentId);
        IEnumerable<Equipment> GetAll();
        IEnumerable<Equipment> GetByLaboratoryId(string labId);
    }
}
