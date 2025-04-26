using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Core;
using WebApplication1.Models.Services;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class LaboratoryController : Controller
    {
        private AccountController _accountController;
        private NotificationService _notificationService;
        private static List<Laboratory> _laboratories = new List<Laboratory>();

        public LaboratoryController(AccountController accountController)
        {
            _accountController = accountController;
            _notificationService = new NotificationService();
        }

        // 顯示創建實驗室頁面
        [Authorize(Roles = "Professor")]
        public IActionResult Create()
        {
            return View();
        }

        // 處理創建實驗室
        [HttpPost]
        [Authorize(Roles = "Professor")]
        public IActionResult Create(RegisterLabViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 獲取當前用戶ID
                var userID = User.FindFirstValue("UserID");
                // 獲取用戶資料
                var professor = _accountController.GetUser(userID) as Professor;

                if (professor != null)
                {
                    // 創建實驗室
                    var lab = professor.CreateLaboratory(new
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Website = model.Website,
                        ContactInfo = model.ContactInfo
                    });

                    // 添加實驗室到列表
                    _laboratories.Add(lab);

                    return RedirectToAction("Details", new { id = lab.LabID });
                }
            }

            return View(model);
        }

        public IActionResult RemoveMember(string labID, string memberID)
        {
            // 獲取當前用戶ID
            var userID = User.FindFirstValue("UserID");
            // 獲取用戶資料
            var professor = _accountController.GetUser(userID) as Professor;

            if (professor != null)
            {
                // 檢查是否為該實驗室的創建者
                var lab = _laboratories.Find(l => l.LabID == labID);
                if (lab != null && lab.Creator.UserID == userID)
                {
                    // 刪除成員
                    professor.RemoveMember(labID, memberID);
                    return RedirectToAction("Details", new { id = labID });
                }
            }

            return Forbid();
        }

        // 顯示實驗室詳情
        public IActionResult Details(string id)
        {
            var lab = _laboratories.Find(l => l.LabID == id);
            if (lab == null)
                return NotFound();

            return View(lab);
        }

        // 顯示添加成員頁面
        [Authorize(Roles = "Professor")]
        public IActionResult AddMember(string id)
        {
            var model = new AddMemberViewModel { LabID = id };
            return View(model);
        }

        // 處理添加成員
        [HttpPost]
        [Authorize(Roles = "Professor")]
        public IActionResult AddMember(AddMemberViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 獲取當前用戶ID
                var userID = User.FindFirstValue("UserID");
                // 獲取用戶資料
                var professor = _accountController.GetUser(userID) as Professor;

                if (professor != null)
                {
                    // 添加成員
                    professor.AddMember(model.LabID, new
                    {
                        Username = model.Username,
                        Email = model.Email
                    });

                    // 找到新添加的學生
                    var lab = _laboratories.Find(l => l.LabID == model.LabID);
                    var student = lab.Members[lab.Members.Count - 1] as Student;

                    // 將學生添加到 AccountController 的用戶列表
                    _accountController.AddUser(student);

                    // 發送通知給新學生
                    _notificationService.NotifyNewMember(student, new { Password = student.Password });

                    return RedirectToAction("Details", new { id = model.LabID });
                }
            }

            return View(model);
        }
    }
}
