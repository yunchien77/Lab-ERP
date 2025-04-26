using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Core
{
    public class User
    {
        [Key]
        public string UserID { get; set; }

        [Required]
        [Display(Name = "使用者名稱")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "電子郵件")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "角色")]
        public string Role { get; set; }  // "Professor" 或 "Student"

        // 建構子
        public User()
        {
            UserID = Guid.NewGuid().ToString();
        }

        // 方法
        public bool Login(string username, string password)
        {
            // 在實際應用中，這裡應該會與資料庫進行驗證
            // 這裡僅做示範
            return Username == username && Password == password;
        }

        public void Logout()
        {
            // 登出邏輯，實際上通常由控制器處理
        }

        public bool UpdateProfile(object userInfo)
        {
            // 更新用戶資料的邏輯
            return true;
        }

        public bool ChangePassword(string oldPassword, string newPassword)
        {
            if (Password == oldPassword)
            {
                Password = newPassword;
                return true;
            }
            return false;
        }

        public bool UpdatePersonalInfo(object personalInfo)
        {
            // 更新個人資料的邏輯
            return true;
        }
    }
}
