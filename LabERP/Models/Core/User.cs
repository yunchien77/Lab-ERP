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
    }
}
