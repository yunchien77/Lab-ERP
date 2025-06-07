using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.Handlers;
using LabERP.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LabERP.Controllers
{
    public class FinanceController : Controller
    {
        private readonly FinanceHandler _financeHandler;
        private readonly ILaboratoryRepository _laboratoryRepository;
        private readonly IUserRepository _userRepository;

        public FinanceController(FinanceHandler financeHandler,
                               ILaboratoryRepository laboratoryRepository,
                               IUserRepository userRepository)
        {
            _financeHandler = financeHandler;
            _laboratoryRepository = laboratoryRepository;
            _userRepository = userRepository;
        }

        // 統一使用 LabID 作為參數名稱
        public IActionResult Index(string LabID)
        {
            // 添加調試信息
            Console.WriteLine($"Received LabID parameter: {LabID}");
            Console.WriteLine($"All route values: {string.Join(", ", RouteData.Values.Select(x => $"{x.Key}={x.Value}"))}");
            Console.WriteLine($"All query parameters: {string.Join(", ", Request.Query.Select(x => $"{x.Key}={x.Value}"))}");

            // 參數檢查
            if (string.IsNullOrEmpty(LabID))
            {
                Console.WriteLine("LabID is null or empty");
                TempData["ErrorMessage"] = "實驗室ID不能為空";
                return RedirectToAction("Dashboard", "User");
            }

            var laboratory = _laboratoryRepository.GetById(LabID);
            Console.WriteLine($"Laboratory found: {laboratory != null}");

            if (laboratory == null)
            {
                Console.WriteLine($"Laboratory not found for ID: {LabID}");
                TempData["ErrorMessage"] = "實驗室不存在";
                return RedirectToAction("Dashboard", "User");
            }

            // 安全地獲取財務數據
            decimal totalIncome = 0;
            decimal totalExpense = 0;
            decimal balance = 0;
            List<FinanceRecord> recentRecords = new List<FinanceRecord>();
            BankAccount bankAccount = null;
            List<Salary> salaries = new List<Salary>();

            try
            {
                totalIncome = _financeHandler.GetTotalIncome(LabID);
                totalExpense = _financeHandler.GetTotalExpense(LabID);
                balance = _financeHandler.GetBalance(LabID);
                recentRecords = _financeHandler.GetFinanceRecords(LabID)?.Take(10).ToList() ?? new List<FinanceRecord>();
                bankAccount = _financeHandler.GetBankAccount(LabID);
                salaries = _financeHandler.GetSalaries(LabID) ?? new List<Salary>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting finance data: {ex.Message}");
                // 繼續使用預設值
            }

            var viewModel = new FinanceManagementViewModel
            {
                LaboratoryId = LabID,
                LaboratoryName = laboratory.Name ?? "未知實驗室",
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                Balance = balance,
                RecentRecords = recentRecords,
                BankAccount = bankAccount,
                Salaries = salaries,
                Creator = laboratory.Creator
            };

            Console.WriteLine($"ViewModel created successfully");
            return View(viewModel);
        }

        public IActionResult AddIncome(string LabID)
        {
            Console.WriteLine($"AddIncome - Received LabID parameter: {LabID}");

            if (string.IsNullOrEmpty(LabID))
            {
                TempData["ErrorMessage"] = "實驗室ID不能為空";
                return RedirectToAction("Dashboard", "User");
            }

            var viewModel = new AddIncomeViewModel
            {
                LaboratoryId = LabID
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddIncome(AddIncomeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var professorId = GetCurrentProfessorId();

                if (string.IsNullOrEmpty(professorId))
                {
                    TempData["ErrorMessage"] = "無法取得當前用戶資訊";
                    return View(model);
                }

                _financeHandler.AddIncome(model.LaboratoryId, model.Amount,
                                        model.Description, model.Category, professorId);

                TempData["SuccessMessage"] = "收入記錄新增成功";
                return RedirectToAction("Index", new { LabID = model.LaboratoryId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"新增收入失敗: {ex.Message}");
                return View(model);
            }
        }

        public IActionResult SalaryManagement(string LabID)
        {
            Console.WriteLine($"SalaryManagement - Received LabID parameter: {LabID}");

            if (string.IsNullOrEmpty(LabID))
            {
                TempData["ErrorMessage"] = "實驗室ID不能為空";
                return RedirectToAction("Dashboard", "User");
            }

            var laboratory = _laboratoryRepository.GetById(LabID);
            if (laboratory == null)
            {
                TempData["ErrorMessage"] = "找不到指定的實驗室";
                return RedirectToAction("Dashboard", "User");
            }

            var salaries = _financeHandler.GetSalaries(LabID) ?? new List<Salary>();
            var labMembers = laboratory.Members ?? new List<User>();

            var viewModel = new SalaryManagementViewModel
            {
                LaboratoryId = LabID,
                LaboratoryName = laboratory.Name ?? "未知實驗室",
                TotalMembers = labMembers.Count,
                SalariesSet = salaries.Count,
                TotalMonthlySalary = salaries.Sum(s => s.Amount),
                SalaryItems = new List<SalaryItem>()
            };

            foreach (var member in labMembers.Where(m => m.Role == "Student"))
            {
                var existingSalary = salaries.FirstOrDefault(s => s.UserId == member.UserID);
                viewModel.SalaryItems.Add(new SalaryItem
                {
                    SalaryId = existingSalary?.Id ?? 0,
                    UserId = member.UserID,
                    UserName = member.Username ?? "未知用戶",
                    CurrentAmount = existingSalary?.Amount ?? 0,
                    PaymentDate = existingSalary?.PaymentDate ?? DateTime.Now.AddMonths(1),
                    Status = existingSalary?.Status ?? "未設定",
                    NewAmount = existingSalary?.Amount ?? 0
                });
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateSalary(string LabID, string userId, string userName, decimal amount)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "薪水須設定大於 0";
                return RedirectToAction("SalaryManagement", new { LabID });
            }

            try
            {
                var professorId = GetCurrentProfessorId();
                if (string.IsNullOrEmpty(professorId))
                {
                    TempData["ErrorMessage"] = "無法取得當前用戶資訊";
                    return RedirectToAction("SalaryManagement", new { LabID });
                }

                _financeHandler.SetSalary(LabID, userId, userName, amount, professorId);

                TempData["SuccessMessage"] = $"已成功更新 {userName} 的薪資";
                return RedirectToAction("SalaryManagement", new { LabID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"更新薪資失敗: {ex.Message}";
                return RedirectToAction("SalaryManagement", new { LabID });
            }
        }

        public IActionResult BankAccountSettings(string LabID)
        {
            Console.WriteLine($"BankAccountSettings - Received LabID parameter: {LabID}");

            if (string.IsNullOrEmpty(LabID))
            {
                TempData["ErrorMessage"] = "實驗室ID不能為空";
                return RedirectToAction("Dashboard", "User");
            }

            var existingAccount = _financeHandler.GetBankAccount(LabID);

            var viewModel = new BankAccountViewModel
            {
                LaboratoryId = LabID
            };

            if (existingAccount != null)
            {
                viewModel.BankName = existingAccount.BankName;
                viewModel.AccountNumber = existingAccount.AccountNumber;
                viewModel.AccountHolder = existingAccount.AccountHolder;
                viewModel.BranchCode = existingAccount.BranchCode;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BankAccountSettings(BankAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _financeHandler.SetBankAccount(model.LaboratoryId, model.BankName,
                                             model.AccountNumber, model.AccountHolder, model.BranchCode);

                TempData["SuccessMessage"] = "銀行帳戶設定成功";
                return RedirectToAction("Index", new { LabID = model.LaboratoryId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"設定銀行帳戶失敗: {ex.Message}");
                return View(model);
            }
        }

        private string GetCurrentProfessorId()
        {
            try
            {
                return User.FindFirstValue("UserID");
            }
            catch
            {
                return null;
            }
        }
    }
}