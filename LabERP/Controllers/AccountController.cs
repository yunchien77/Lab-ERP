using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Core;
using WebApplication1.Models.ViewModels;
using WebApplication1.Models.Handlers;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountHandler _accountManager;

        public AccountController(AccountHandler accountManager)
        {
            _accountManager = accountManager;
        }

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
                var user = _accountManager.AuthenticateUser(model.Username, model.Password);
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

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
}
}
