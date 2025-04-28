using WebApplication1.Models.Core;

namespace WebApplication1.Interface
{
    public interface IUserRepository
    {
        User FindUser(string username, string password);
        User GetUserById(string userId);
        void Add(User user);
        void Update(User user);
        void Delete(string userId);
        IEnumerable<User> GetAllUsers();
    }
}
