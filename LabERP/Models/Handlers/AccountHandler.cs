using LabERP.Interface;
using LabERP.Interface;
using LabERP.Models.Core;

namespace LabERP.Models.Handlers
{
    public class AccountHandler : IAccountHandler
    {
        private readonly IUserRepository _userRepository;

        public AccountHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User? AuthenticateUser(string username, string password)
        {
            return _userRepository.FindUser(username, password);
        }

        public void RegisterUser(User user)
        {
            // 可以在這裡添加業務邏輯，如密碼加密、驗證等
            _userRepository.Add(user);
        }

        public User GetUser(string userId)
        {
            return _userRepository.GetUserById(userId);
        }
    }
}
