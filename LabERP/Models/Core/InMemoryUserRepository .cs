using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemoryUserRepository : IUserRepository
    {
        private static List<User> _users = new List<User>();
        private static bool _initialized = false;

        public InMemoryUserRepository()
        {
            if (!_initialized)
            {
                InitializeData();
                _initialized = true;
            }
        }

        private void InitializeData()
        {
            // 創建教授
            var prof1 = new Professor
            {
                UserID = "1",
                Username = "prof1",
                Password = "111",
                Email = "prof1@example.com"
            };

            var prof2 = new Professor
            {
                UserID = "2",
                Username = "prof2",
                Password = "password",
                Email = "prof2@example.com"
            };

            // 為 prof1 創建初始實驗室
            var initialLab = new Laboratory
            {
                Name = "人工智慧實驗室",
                Description = "專注於機器學習與深度學習研究",
                Website = "https://ai-lab.example.com",
                ContactInfo = "ai-lab@example.com",
                Creator = prof1
            };

            // 將實驗室加入教授的實驗室列表
            prof1.Laboratories.Add(initialLab);

            // 將教授加入用戶列表
            _users.Add(prof1);
            _users.Add(prof2);

            // 同時將實驗室加入到實驗室儲存庫
            var labRepo = new InMemoryLaboratoryRepository();
            labRepo.Add(initialLab);
        }

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