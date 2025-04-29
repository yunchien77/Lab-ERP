using LabERP.Models.Core;
using LabERP.Interface;

namespace LabERP.Models.Handlers
{
    public class UserHandler : IUserHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly ILaboratoryRepository _laboratoryRepository;

        public UserHandler(IUserRepository userRepository, ILaboratoryRepository laboratoryRepository)
        {
            _userRepository = userRepository;
            _laboratoryRepository = laboratoryRepository;
        }

        public User GetUserById(string userId)
        {
            return _userRepository.GetUserById(userId);
        }

        public bool ChangePassword(string userId, string oldPassword, string newPassword)
        {
            var user = _userRepository.GetUserById(userId);
            if (user == null)
            {
                return false;
            }

            if (user.Password != oldPassword)
            {
                return false;
            }

            user.Password = newPassword;
            _userRepository.Update(user);
            return true;
        }

        public bool UpdateStudentProfile(string userId, StudentProfileDto profileInfo)
        {
            var student = _userRepository.GetUserById(userId) as Student;
            if (student == null)
            {
                return false;
            }

            student.StudentID = profileInfo.StudentID;
            student.PhoneNumber = profileInfo.PhoneNumber;
            _userRepository.Update(student);
            return true;
        }

        public IEnumerable<Laboratory> GetStudentLaboratories(string studentId)
        {
            // 從所有實驗室中找出包含此學生的實驗室
            return _laboratoryRepository.GetAll()
                .Where(lab => lab.Members.Any(m => m.UserID == studentId))
                .ToList();
        }
    }
}
