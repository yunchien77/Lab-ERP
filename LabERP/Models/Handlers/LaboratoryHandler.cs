using WebApplication1.Interface;
using WebApplication1.Models.Core;

namespace WebApplication1.Models.Handlers
{
    public class LaboratoryHandler
    {
        private readonly ILaboratoryRepository _laboratoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        public LaboratoryHandler(
            ILaboratoryRepository laboratoryRepository,
            IUserRepository userRepository,
            INotificationService notificationService)
        {
            _laboratoryRepository = laboratoryRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        // 創建實驗室
        public Laboratory CreateLaboratory(string professorId, LaboratoryCreateDto labInfo)
        {
            var professor = _userRepository.GetUserById(professorId) as Professor;
            if (professor == null)
            {
                throw new InvalidOperationException("只有教授可以創建實驗室");
            }

            var laboratory = new Laboratory
            {
                Name = labInfo.Name,
                Description = labInfo.Description,
                Website = labInfo.Website,
                ContactInfo = labInfo.ContactInfo,
                Creator = professor
            };

            // 保存實驗室
            _laboratoryRepository.Add(laboratory);

            // 更新教授的實驗室列表
            professor.Laboratories.Add(laboratory);
            _userRepository.Update(professor);

            return laboratory;
        }

        // 更新實驗室
        public Laboratory UpdateLaboratory(string professorId, string labId, LaboratoryUpdateDto labInfo)
        {
            var professor = _userRepository.GetUserById(professorId) as Professor;
            var laboratory = _laboratoryRepository.GetById(labId);

            if (professor == null || laboratory == null || laboratory.Creator.UserID != professorId)
            {
                throw new InvalidOperationException("無權限更新此實驗室");
            }

            // 更新實驗室信息
            laboratory.Name = labInfo.Name ?? laboratory.Name;
            laboratory.Description = labInfo.Description ?? laboratory.Description;
            laboratory.Website = labInfo.Website ?? laboratory.Website;
            laboratory.ContactInfo = labInfo.ContactInfo ?? laboratory.ContactInfo;

            // 保存更新
            _laboratoryRepository.Update(laboratory);

            return laboratory;
        }

        // 添加成員
        public Student AddMember(string professorId, string labId, MemberCreateDto memberInfo)
        {
            var professor = _userRepository.GetUserById(professorId) as Professor;
            var laboratory = _laboratoryRepository.GetById(labId);

            if (professor == null || laboratory == null || laboratory.Creator.UserID != professorId)
            {
                throw new InvalidOperationException("無權限添加成員到此實驗室");
            }

            // 創建新學生
            var student = new Student
            {
                Username = memberInfo.Username,
                Email = memberInfo.Email,
                Password = GenerateRandomPassword(),
                Role = "Student"
            };

            // 將學生添加到實驗室
            laboratory.AddMember(student);
            _laboratoryRepository.Update(laboratory);

            // 將學生添加到用戶儲存庫
            _userRepository.Add(student);

            // 發送通知
            _notificationService.NotifyNewMember(student, new { Password = student.Password });

            return student;
        }

        // 刪除成員
        public bool RemoveMember(string professorId, string labId, string memberId)
        {
            var professor = _userRepository.GetUserById(professorId) as Professor;
            var laboratory = _laboratoryRepository.GetById(labId);

            if (professor == null || laboratory == null || laboratory.Creator.UserID != professorId)
            {
                throw new InvalidOperationException("無權限從此實驗室刪除成員");
            }

            // 刪除成員
            var result = laboratory.RemoveMember(memberId);
            if (result)
            {
                _laboratoryRepository.Update(laboratory);
            }
            return result;
        }

        // 獲取實驗室詳情
        public Laboratory GetLaboratoryDetails(string labId)
        {
            return _laboratoryRepository.GetById(labId);
        }

        // 獲取教授的所有實驗室
        public IEnumerable<Laboratory> GetProfessorLaboratories(string professorId)
        {
            return _laboratoryRepository.GetByCreator(professorId);
        }

        // 生成隨機密碼
        private string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}
