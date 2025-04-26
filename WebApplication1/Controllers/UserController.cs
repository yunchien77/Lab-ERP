using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Core;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private AccountController _accountController;

        public UserController(AccountController accountController)
        {
            _accountController = accountController;
        }

        // 用戶儀表板
        public IActionResult Dashboard()
        {
            // 獲取當前用戶ID
            var userID = User.FindFirstValue("UserID");
            // 獲取用戶資料
            var user = _accountController.GetUser(userID);

            return View(user);
        }

        // 顯示修改密碼頁面
        public IActionResult ChangePassword()
        {
            return View();
        }

        // 處理修改密碼
        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword)
        {
            if (ModelState.IsValid)
            {
                // 獲取當前用戶ID
                var userID = User.FindFirstValue("UserID");
                // 獲取用戶資料
                var user = _accountController.GetUser(userID);

                if (user != null)
                {
                    // 嘗試修改密碼
                    if (user.ChangePassword(oldPassword, newPassword))
                    {
                        ViewBag.Message = "密碼已成功修改。";
                        return View();
                    }

                    ModelState.AddModelError("", "舊密碼不正確。");
                }
            }

            return View();
        }
    }
}
