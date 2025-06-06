using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.ViewModels;

namespace LabERP.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserHandler _userHandler;

        public UserController(IUserHandler userHandler)
        {
            _userHandler = userHandler;
        }

        // 用戶儀表板
        public IActionResult Dashboard()
        {
            // 獲取當前用戶ID
            var userID = User.FindFirstValue("UserID");

            // 獲取用戶資料
            var user = _userHandler.GetUserById(userID);

            // 如果是學生，則獲取其實驗室資訊
            if (User.IsInRole("Student"))
            {
                ViewBag.StudentLaboratories = _userHandler.GetStudentLaboratories(userID);
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

                // 嘗試修改密碼
                if (_userHandler.ChangePassword(userID, oldPassword, newPassword))
                {
                    ViewBag.Message = "密碼已成功修改。";
                    return View();
                }

                ModelState.AddModelError("", "舊密碼不正確。");
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
            var student = _userHandler.GetUserById(userID) as Student;
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

                // 更新學生資料
                var profileInfo = new StudentProfileDto
                {
                    StudentID = model.StudentID,
                    PhoneNumber = model.PhoneNumber
                };

                if (_userHandler.UpdateStudentProfile(userID, profileInfo))
                {
                    ViewBag.Message = "個人資料已成功更新。";
                    return View(model);
                }
            }

            return View(model);
        }
    }
}
