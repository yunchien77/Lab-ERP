using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Core;
using WebApplication1.Models.ViewModels;

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

            if (string.IsNullOrEmpty(userID))
            {
                // 如果 userID 為 null，重定向到登入頁面
                return RedirectToAction("Login", "Account");
            }
            // 獲取用戶資料
            var user = _accountController.GetUser(userID);

            // 檢查 user 是否為 null
            if (user == null)
            {
                // 如果找不到用戶，重定向到登入頁面
                return RedirectToAction("Login", "Account");
            }

            // 如果是學生，則獲取其實驗室資訊
            if (User.IsInRole("Student"))
            {
                ViewBag.StudentLaboratories = GetStudentLaboratories(userID);
            }
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

        // 顯示修改個人資料頁面
        [Authorize(Roles = "Student")]
        public IActionResult EditProfile()
        {
            // 獲取當前用戶ID
            var userID = User.FindFirstValue("UserID");
            // 獲取用戶資料
            var student = _accountController.GetUser(userID) as Student;

            if (student == null)
                return NotFound();

            var model = new EditProfileViewModel
            {
                StudentID = student.StudentID,
                PhoneNumber = student.PhoneNumber
            };

            return View(model);
        }

        // 處理修改個人資料
        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 獲取當前用戶ID
                var userID = User.FindFirstValue("UserID");
                // 獲取用戶資料
                var student = _accountController.GetUser(userID) as Student;

                if (student != null)
                {
                    // 更新學生資料
                    student.StudentID = model.StudentID;
                    student.PhoneNumber = model.PhoneNumber;

                    ViewBag.Message = "個人資料已成功更新。";
                    return View(model);
                }
            }

            return View(model);
        }

        // 取得學生所屬的實驗室
        private List<Laboratory> GetStudentLaboratories(string studentID)
        {
            // 從所有實驗室中找出包含此學生的實驗室
            return LaboratoryController._laboratories.Where(
                lab => lab.Members.Any(m => m.UserID == studentID)
            ).ToList();
        }
    }
}
