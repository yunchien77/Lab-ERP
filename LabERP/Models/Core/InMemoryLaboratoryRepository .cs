using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemoryLaboratoryRepository : ILaboratoryRepository
    {
        private static List<Laboratory> _laboratories = new List<Laboratory>();

        public Laboratory GetById(string labId)
        {
            return _laboratories.Find(l => l.LabID == labId);
        }

        public void Add(Laboratory laboratory)
        {
            if (_laboratories.Any(l => l.LabID == laboratory.LabID))
            {
                throw new ArgumentException($"實驗室ID {laboratory.LabID} 已存在");
            }
            _laboratories.Add(laboratory);
        }

        public void Update(Laboratory laboratory)
        {
            var index = _laboratories.FindIndex(l => l.LabID == laboratory.LabID);
            if (index != -1)
            {
                _laboratories[index] = laboratory;
            }
        }

        public IEnumerable<Laboratory> GetAll()
        {
            return _laboratories.ToList();
        }

        public IEnumerable<Laboratory> GetByCreator(string professorId)
        {
            return _laboratories.Where(l => l.Creator.UserID == professorId).ToList();
        }
    }
}
