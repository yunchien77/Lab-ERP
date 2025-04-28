using WebApplication1.Models.Core;

namespace WebApplication1.Interface
{
    public interface INotificationService
    {
        bool NotifyNewMember(Student student, object credentials);
        bool SendCredentials(object credentials);
    }
}
