using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IUserHandler
    {
        User GetUserById(string userId);
        bool ChangePassword(string userId, string oldPassword, string newPassword);
        bool UpdateStudentProfile(string userId, StudentProfileDto profileInfo);
        IEnumerable<Laboratory> GetStudentLaboratories(string studentId);
    }
}
