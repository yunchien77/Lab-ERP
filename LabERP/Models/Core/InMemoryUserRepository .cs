using WebApplication1.Interface;

namespace WebApplication1.Models.Core
{
    public class InMemoryUserRepository : IUserRepository
    {
        private static List<User> _users = new List<User>
    {
        new Professor
        {
            UserID = "1",
            Username = "prof1",
            Password = "password",
            Email = "prof1@example.com"
        },
        new Professor
        {
            UserID = "2",
            Username = "prof2",
            Password = "password",
            Email = "prof2@example.com"
        }
    };
        public User FindUser(string username, string password)
        {
            return _users.Find(u => u.Username == username && u.Password == password);
        }

        public User GetUserById(string userId)
        {
            return _users.Find(u => u.UserID == userId);
        }

        public void Add(User user)
        {
            if (_users.Any(u => u.UserID == user.UserID))
            {
                throw new ArgumentException($"使用者ID {user.UserID} 已存在");
            }
            _users.Add(user);
        }

        public User FindByUsername(string username)
        {
            return _users.Find(u => u.Username == username);
        }

        public void Update(User user)
        {
            var index = _users.FindIndex(u => u.UserID == user.UserID);
            if (index != -1)
            {
                _users[index] = user;
            }
        }

        public void Delete(string userId)
        {
            _users.RemoveAll(u => u.UserID == userId);
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _users.ToList(); // 返回副本避免外部修改
        }
    }
}
