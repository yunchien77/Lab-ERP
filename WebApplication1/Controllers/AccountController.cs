using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Core;
using WebApplication1.Models.ViewModels;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        // 模擬用戶資料庫
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

        // 登入頁面
        public IActionResult Login()
        {
            return View();
        }

        // 處理登入
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 查找用戶
                var user = _users.Find(u => u.Username == model.Username && u.Password == model.Password);

                if (user != null)
                {
                    // 建立身份驗證票據
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("UserID", user.UserID)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe
                        });

                    return RedirectToAction("Dashboard", "User");
                }

                ModelState.AddModelError("", "使用者名稱或密碼不正確");
            }

            return View(model);
        }

        // 登出
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // 添加新用戶 (模擬用於添加學生)
        public void AddUser(User user)
        {
            _users.Add(user);
        }

        // 獲取用戶
        public User GetUser(string userID)
        {
            return _users.Find(u => u.UserID == userID);
        }
    }
}
