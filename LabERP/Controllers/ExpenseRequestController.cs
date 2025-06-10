using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.Handlers;
using LabERP.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LabERP.Controllers
{
    public class ExpenseRequestController : Controller
    {
        private readonly ExpenseRequestHandler _expenseRequestHandler;
        private readonly ILaboratoryRepository _laboratoryRepository;
        private readonly IFinanceRepository _financeRepository;
        private readonly IUserRepository _userRepository;

        public ExpenseRequestController(
            ExpenseRequestHandler expenseRequestHandler,
            ILaboratoryRepository laboratoryRepository,
            IFinanceRepository financeRepository,
            IUserRepository userRepository)
        {
            _expenseRequestHandler = expenseRequestHandler;
            _laboratoryRepository = laboratoryRepository;
            _financeRepository = financeRepository;
            _userRepository = userRepository;
        }

        public IActionResult Index(string LabID)
        {
            if (string.IsNullOrEmpty(LabID))
            {
                TempData["ErrorMessage"] = "實驗室ID不能為空";
               // return RedirectToAction("Dashboard", "User");
            }

            var laboratory = _laboratoryRepository.GetById(LabID);
            if (laboratory == null)
            {
                TempData["ErrorMessage"] = "找不到指定的實驗室";
               // return RedirectToAction("Dashboard", "User");
            }

            var currentUserId = User.FindFirst("UserID")?.Value;
            //Console.WriteLine(currentUserId);
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            //Console.WriteLine(currentUserRole);
            var isProfessor = currentUserRole == "Professor";
            //Console.WriteLine(laboratory.Creator.UserID);
            var isLabCreator = isProfessor && laboratory.Creator?.UserID == currentUserId;

            // 檢查用戶是否為實驗室成員
            if (!laboratory.Members.Any(m => m.UserID == currentUserId))
            {
                TempData["ErrorMessage"] = "您不是此實驗室的成員";
                //return RedirectToAction("Dashboard", "User");
            }

            var expenseRequests = _expenseRequestHandler.GetExpenseRequestsByLaboratory(LabID);
            var availableBudget = _financeRepository.GetBalance(LabID);

            // 如果是學生，只顯示自己的報帳申請
            if (!isProfessor)
            {
                expenseRequests = expenseRequests.Where(e => e.RequesterId == currentUserId).ToList();
            }

            var viewModel = new ExpenseRequestListViewModel
            {
                LaboratoryId = LabID,
                LaboratoryName = laboratory.Name,
                CurrentUserId = currentUserId,
                CurrentUserRole = currentUserRole,
                IsProfessor = isProfessor,
                IsLabCreator = isLabCreator,
                AvailableBudget = availableBudget,
                ExpenseRequests = expenseRequests.Select(e => new ExpenseRequestItem
                {
                    Id = e.Id,
                    RequesterName = e.RequesterName,
                    Amount = e.Amount,
                    Category = e.Category,
                    Description = e.Description,
                    Status = e.Status,
                    RequestDate = e.RequestDate,
                    ReviewDate = e.ReviewDate,
                    ReviewNotes = e.ReviewNotes,
                    CanDelete = !isProfessor && e.RequesterId == currentUserId && e.Status == ExpenseRequestStatus.Pending,
                    AttachmentCount = e.Attachments?.Count ?? 0
                }).ToList(),
                PendingCount = expenseRequests.Count(e => e.Status == ExpenseRequestStatus.Pending),
                ApprovedCount = expenseRequests.Count(e => e.Status == ExpenseRequestStatus.Approved),
                RejectedCount = expenseRequests.Count(e => e.Status == ExpenseRequestStatus.Rejected)
            };

            return View(viewModel);
        }

        // GET: ExpenseRequest/Create/{laboratoryId}
        [Authorize(Roles = "Student")]
        public IActionResult Create(string laboratoryId)
        {
            if (string.IsNullOrEmpty(laboratoryId))
            {
                TempData["ErrorMessage"] = "實驗室ID不能為空";
                return RedirectToAction("Dashboard", "User");
            }

            var laboratory = _laboratoryRepository.GetById(laboratoryId);
            if (laboratory == null)
            {
                TempData["ErrorMessage"] = "找不到指定的實驗室";
                return RedirectToAction("Dashboard", "User");
            }

            var currentUserId = User.FindFirst("UserID")?.Value;
            if (!laboratory.Members.Any(m => m.UserID == currentUserId))
            {
                TempData["ErrorMessage"] = "您不是此實驗室的成員";
                return RedirectToAction("Dashboard", "User");
            }

            var viewModel = new CreateExpenseRequestViewModel
            {
                LaboratoryId = laboratoryId,
                LaboratoryName = laboratory.Name
            };

            return View(viewModel);
        }

        // POST: ExpenseRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public IActionResult Create(CreateExpenseRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var laboratory = _laboratoryRepository.GetById(model.LaboratoryId);
            if (laboratory == null)
            {
                TempData["ErrorMessage"] = "找不到指定的實驗室";
                return RedirectToAction("Dashboard", "User");
            }

            var currentUserId = User.FindFirst("UserID")?.Value;
            var currentUser = _userRepository.GetUserById(currentUserId);

            if (currentUser == null || !laboratory.Members.Any(m => m.UserID == currentUserId))
            {
                TempData["ErrorMessage"] = "您不是此實驗室的成員";
                return RedirectToAction("Dashboard", "User");
            }

            try
            {
                _expenseRequestHandler.CreateExpenseRequest(
                    model.LaboratoryId,
                    currentUserId,
                    currentUser.Username,
                    model.Amount,
                    model.InvoiceNumber,
                    model.Category,
                    model.Description,
                    model.Purpose,
                    model.Attachments
                );

                TempData["SuccessMessage"] = "報帳申請已成功提交";
                return RedirectToAction("Index", new { LabID = model.LaboratoryId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"提交報帳申請時發生錯誤: {ex.Message}";
                return View(model);
            }
        }

        // GET: ExpenseRequest/Details/{id}
        public IActionResult Details(int id)
        {
            var expenseRequest = _expenseRequestHandler.GetExpenseRequestWithAttachments(id);
            if (expenseRequest == null)
            {
                TempData["ErrorMessage"] = "找不到指定的報帳申請";
                return RedirectToAction("Dashboard", "User");
            }

            var laboratory = _laboratoryRepository.GetById(expenseRequest.LaboratoryId);
            if (laboratory == null)
            {
                TempData["ErrorMessage"] = "找不到相關的實驗室";
                return RedirectToAction("Dashboard", "User");
            }

            var currentUserId = User.FindFirst("UserID")?.Value;
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var isProfessor = currentUserRole == "Professor";

            // 檢查權限
            if (!isProfessor && expenseRequest.RequesterId != currentUserId)
            {
                TempData["ErrorMessage"] = "您沒有權限查看此報帳申請";
                return RedirectToAction("Dashboard", "User");
            }

            var availableBudget = _financeRepository.GetBalance(expenseRequest.LaboratoryId);

            var viewModel = new ExpenseRequestDetailViewModel
            {
                ExpenseRequest = expenseRequest,
                LaboratoryName = laboratory.Name,
                CanDelete = !isProfessor && expenseRequest.RequesterId == currentUserId && expenseRequest.Status == ExpenseRequestStatus.Pending,
                CanReview = isProfessor && laboratory.Creator?.UserID == currentUserId && expenseRequest.Status == ExpenseRequestStatus.Pending,
                CurrentUserId = currentUserId,
                AvailableBudget = availableBudget
            };

            return View(viewModel);
        }

        // GET: ExpenseRequest/Review/{id}
        [Authorize(Roles = "Professor")]
        public IActionResult Review(int id)
        {
            var expenseRequest = _expenseRequestHandler.GetExpenseRequestWithAttachments(id);
            if (expenseRequest == null)
            {
                TempData["ErrorMessage"] = "找不到指定的報帳申請";
                return RedirectToAction("Dashboard", "User");
            }

            if (expenseRequest.Status != ExpenseRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "此報帳申請已經審核過了";
                return RedirectToAction("Details", new { id });
            }

            var laboratory = _laboratoryRepository.GetById(expenseRequest.LaboratoryId);
            if (laboratory == null)
            {
                TempData["ErrorMessage"] = "找不到相關的實驗室";
                return RedirectToAction("Dashboard", "User");
            }

            var currentUserId = User.FindFirst("UserID")?.Value;
            if (laboratory.Creator?.UserID != currentUserId)
            {
                TempData["ErrorMessage"] = "只有實驗室創建者可以審核報帳申請";
                return RedirectToAction("Dashboard", "User");
            }

            var availableBudget = _financeRepository.GetBalance(expenseRequest.LaboratoryId);

            var viewModel = new ReviewExpenseRequestViewModel
            {
                ExpenseRequestId = expenseRequest.Id,
                LaboratoryId = expenseRequest.LaboratoryId,
                LaboratoryName = laboratory.Name,
                RequesterName = expenseRequest.RequesterName,
                Amount = expenseRequest.Amount,
                InvoiceNumber = expenseRequest.InvoiceNumber,
                Category = expenseRequest.Category,
                Description = expenseRequest.Description,
                Purpose = expenseRequest.Purpose,
                RequestDate = expenseRequest.RequestDate,
                Attachments = expenseRequest.Attachments,
                AvailableBudget = availableBudget,
                InsufficientBudget = expenseRequest.Amount > availableBudget
            };

            return View(viewModel);
        }

        // POST: ExpenseRequest/Review
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Professor")]
        public IActionResult Review(ReviewExpenseRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // 重新載入必要資料
                var expenseRequest = _expenseRequestHandler.GetExpenseRequestWithAttachments(model.ExpenseRequestId);
                if (expenseRequest != null)
                {
                    model.Attachments = expenseRequest.Attachments;
                    model.AvailableBudget = _financeRepository.GetBalance(expenseRequest.LaboratoryId);
                    model.InsufficientBudget = expenseRequest.Amount > model.AvailableBudget;
                }
                return View(model);
            }

            var currentUserId = User.FindFirst("UserID")?.Value;

            try
            {
                _expenseRequestHandler.ReviewExpenseRequest(
                    model.ExpenseRequestId,
                    currentUserId,
                    model.Approved.Value,
                    model.ReviewNotes
                );

                TempData["SuccessMessage"] = $"報帳審核完成 - {(model.Approved.Value ? "通過" : "不通過")}";
                return RedirectToAction("Index", new { LabID = model.LaboratoryId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"審核報帳申請時發生錯誤: {ex.Message}";

                // 重新載入必要資料
                var expenseRequest = _expenseRequestHandler.GetExpenseRequestWithAttachments(model.ExpenseRequestId);
                if (expenseRequest != null)
                {
                    model.Attachments = expenseRequest.Attachments;
                    model.AvailableBudget = _financeRepository.GetBalance(expenseRequest.LaboratoryId);
                    model.InsufficientBudget = expenseRequest.Amount > model.AvailableBudget;
                }
                return View(model);
            }
        }

        // POST: ExpenseRequest/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public IActionResult Delete(int id, string LabID)
        {
            var currentUserId = User.FindFirst("UserID")?.Value;

            try
            {
                bool success = _expenseRequestHandler.DeleteExpenseRequest(id, currentUserId);

                if (success)
                {
                    TempData["SuccessMessage"] = "報帳申請已成功刪除";
                }
                else
                {
                    TempData["ErrorMessage"] = "無法刪除此報帳申請，可能已經審核或不是您的申請";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"刪除報帳申請時發生錯誤: {ex.Message}";
            }

            return RedirectToAction("Index", new { LabID });
        }
    }
}
