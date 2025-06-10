using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.ViewModels;

namespace LabERP.Controllers
{
    [Authorize]
    public class WorkSessionController : Controller
    {
        private readonly IWorkSessionHandler _workSessionHandler;
        //private readonly IUserHandler _userHandler;
        private readonly ILaboratoryRepository _laboratoryRepository;

        public WorkSessionController(
            IWorkSessionHandler workSessionHandler,
            //IUserHandler userHandler,
            ILaboratoryRepository laboratoryRepository)
        {
            _workSessionHandler = workSessionHandler;
            //_userHandler = userHandler;
            _laboratoryRepository = laboratoryRepository;
        }

        // 顯示打卡頁面
        [Authorize(Roles = "Student")]
        public IActionResult Index(string labId)
        {
            var userID = User.FindFirstValue("UserID");
            var laboratory = _laboratoryRepository.GetById(labId);

            if (laboratory == null)
            {
                return NotFound("實驗室不存在");
            }

            // 檢查學生是否為實驗室成員
            if (!laboratory.Members.Any(m => m.UserID == userID))
            {
                return Forbid("您不是此實驗室的成員");
            }

            var currentStatus = _workSessionHandler.GetCurrentWorkStatus(userID, labId);

            var model = new WorkSessionViewModel
            {
                LabID = labId,
                LabName = laboratory.Name,
                CurrentStatus = currentStatus,
                StudentID = userID
            };

            return View(model);
        }

        // 開始工作
        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult StartWork(string labId)
        {
            try
            {
                var userID = User.FindFirstValue("UserID");
                var workSession = _workSessionHandler.StartWork(userID, labId);

                TempData["SuccessMessage"] = $"開始工作打卡成功！開始時間：{workSession.StartTime:yyyy-MM-dd HH:mm:ss}";
                return RedirectToAction("Index", new { labId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", new { labId });
            }
        }

        // 結束工作
        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult EndWork(string labId)
        {
            try
            {
                var userID = User.FindFirstValue("UserID");
                var workSession = _workSessionHandler.EndWork(userID, labId);

                TempData["SuccessMessage"] = $"結束工作打卡成功！工作時數：{workSession.GetWorkHours():F2} 小時";
                return RedirectToAction("Index", new { labId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", new { labId });
            }
        }

        // 查看打卡記錄
        [Authorize(Roles = "Student")]
        public IActionResult Records(string labId, DateTime? startDate, DateTime? endDate)
        {
            var userID = User.FindFirstValue("UserID");
            var laboratory = _laboratoryRepository.GetById(labId);

            if (laboratory == null)
            {
                return NotFound("實驗室不存在");
            }

            // 設定預設查詢範圍為本月
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!endDate.HasValue)
            {
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            var workSessions = _workSessionHandler.GetWorkSessions(userID, labId, startDate, endDate);
            var summary = _workSessionHandler.GetWorkSummary(userID, labId, startDate.Value, endDate.Value);

            var model = new WorkSessionRecordsViewModel
            {
                LabID = labId,
                LabName = laboratory.Name,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                WorkSessions = workSessions.ToList(),
                Summary = summary
            };

            return View(model);
        }

        // 教授查看實驗室出勤管理
        [Authorize(Roles = "Professor")]
        public IActionResult LabAttendance(string labId, DateTime? startDate, DateTime? endDate)
        {
            var laboratory = _laboratoryRepository.GetById(labId);
            if (laboratory == null)
            {
                return NotFound("實驗室不存在");
            }

            // 檢查是否是實驗室創建者
            var userID = User.FindFirstValue("UserID");
            if (laboratory.Creator.UserID != userID)
            {
                return Forbid("您沒有權限查看此實驗室的出勤記錄");
            }

            // 設定預設查詢範圍為本月
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!endDate.HasValue)
            {
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            var studentSummaries = _workSessionHandler.GetLabWorkSummary(labId, startDate.Value, endDate.Value);

            var model = new LabAttendanceViewModel
            {
                LabID = labId,
                LabName = laboratory.Name,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                StudentSummaries = studentSummaries.ToList()
            };

            return View(model);
        }

        // 教授查看特定學生的詳細打卡記錄
        [Authorize(Roles = "Professor")]
        public IActionResult StudentRecords(string labId, string studentId, DateTime? startDate, DateTime? endDate)
        {
            var laboratory = _laboratoryRepository.GetById(labId);
            if (laboratory == null)
            {
                return NotFound("實驗室不存在");
            }

            // 檢查是否是實驗室創建者
            var userID = User.FindFirstValue("UserID");
            if (laboratory.Creator.UserID != userID)
            {
                return Forbid("您沒有權限查看此學生的打卡記錄");
            }

            // 檢查學生是否是實驗室成員
            var student = laboratory.Members.FirstOrDefault(m => m.UserID == studentId) as Student;
            if (student == null)
            {
                return NotFound("學生不是此實驗室成員");
            }

            // 設定預設查詢範圍為本月
            if (!startDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            if (!endDate.HasValue)
            {
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            var workSessions = _workSessionHandler.GetWorkSessions(studentId, labId, startDate, endDate);
            var summary = _workSessionHandler.GetWorkSummary(studentId, labId, startDate.Value, endDate.Value);

            var model = new StudentWorkRecordsViewModel
            {
                LabID = labId,
                LabName = laboratory.Name,
                StudentID = studentId,
                StudentName = student.Username,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                WorkSessions = workSessions.ToList(),
                Summary = summary
            };

            return View(model);
        }
    }
}