using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LabERP.Models.Core;
using LabERP.Models.ViewModels;
using LabERP.Interface;
using LabERP.Models.Handlers;
using LabERP.Interface;

namespace LabERP.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountHandler _accountHandler;

        public AccountController(IAccountHandler accountHandler)
        {
            _accountHandler = accountHandler;
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
                var user = _accountHandler.AuthenticateUser(model.Username, model.Password);
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
