using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface INotificationService
    {
        bool NotifyNewMember(Student student, object credentials);
        bool SendCredentials(object credentials);
    }
}
