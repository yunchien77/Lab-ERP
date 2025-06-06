using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemoryEquipmentRepository : IEquipmentRepository
    {
        private static List<Equipment> _equipments = new List<Equipment>();

        public Equipment GetById(string equipmentId)
        {
            return _equipments.Find(e => e.EquipmentID == equipmentId);
        }

        public void Add(Equipment equipment)
        {
            if (_equipments.Any(e => e.EquipmentID == equipment.EquipmentID))
            {
                throw new ArgumentException($"設備ID {equipment.EquipmentID} 已存在");
            }
            _equipments.Add(equipment);
        }

        public void Update(Equipment equipment)
        {
            var index = _equipments.FindIndex(e => e.EquipmentID == equipment.EquipmentID);
            if (index != -1)
            {
                _equipments[index] = equipment;
            }
        }

        public void Delete(string equipmentId)
        {
            _equipments.RemoveAll(e => e.EquipmentID == equipmentId);
        }

        public IEnumerable<Equipment> GetAll()
        {
            return _equipments.ToList(); // 返回副本避免外部修改
        }

        public IEnumerable<Equipment> GetByLaboratoryId(string labId)
        {
            return _equipments.Where(e => e.LaboratoryID == labId).ToList();
        }
    }
}
