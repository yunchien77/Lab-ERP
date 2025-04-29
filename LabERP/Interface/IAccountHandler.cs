using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IAccountHandler
    {
        User? AuthenticateUser(string username, string password);
        void RegisterUser(User user);
        User GetUser(string userId);
    }
}
