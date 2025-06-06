using LabERP.Interface;
using LabERP.Models.Handlers;
using LabERP.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Emit;

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

        // 財務管理主頁面
        public IActionResult Index(string laboratoryId)
        {
            var laboratory = _laboratoryRepository.GetById(laboratoryId);
            if (laboratory == null)
            {
                return NotFound("找不到指定的實驗室");
            }

            var viewModel = new FinanceManagementViewModel
            {
                LaboratoryId = laboratoryId,
                LaboratoryName = laboratory.Name,
                TotalIncome = _financeHandler.GetTotalIncome(laboratoryId),
                TotalExpense = _financeHandler.GetTotalExpense(laboratoryId),
                Balance = _financeHandler.GetBalance(laboratoryId),
                RecentRecords = _financeHandler.GetFinanceRecords(laboratoryId).Take(10).ToList(),
                BankAccount = _financeHandler.GetBankAccount(laboratoryId),
                Salaries = _financeHandler.GetSalaries(laboratoryId)
            };

            return View(viewModel);
        }

        // 新增收入 - GET
        public IActionResult AddIncome(string laboratoryId)
        {
            var viewModel = new AddIncomeViewModel
            {
                LaboratoryId = laboratoryId
            };

            return View(viewModel);
        }

        // 新增收入 - POST
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
                // 假設從Session或Claims取得教授ID
                var professorId = GetCurrentProfessorId();

                _financeHandler.AddIncome(model.LaboratoryId, model.Amount,
                                        model.Description, model.Category, professorId);

                TempData["SuccessMessage"] = "收入記錄新增成功";
                return RedirectToAction("Index", new { laboratoryId = model.LaboratoryId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"新增收入失敗: {ex.Message}");
                return View(model);
            }
        }

        // 薪資管理頁面
        public IActionResult SalaryManagement(string laboratoryId)
        {
            var laboratory = _laboratoryRepository.GetById(laboratoryId.ToString());
            if (laboratory == null)
            {
                return NotFound("找不到指定的實驗室");
            }

            var salaries = _financeHandler.GetSalaries(laboratoryId);
            var labMembers = laboratory.Members;

            var viewModel = new SalaryManagementViewModel
            {
                LaboratoryId = laboratoryId,
                LaboratoryName = laboratory.Name,
                TotalMembers = labMembers.Count, // 計算總人數
                SalariesSet = salaries.Count, // 計算已設定薪資的人數
                TotalMonthlySalary = salaries.Sum(s => s.Amount) // 計算月薪資總額
            };

            foreach (var member in labMembers.Where(m => m.Role == "Student"))
            {
                var existingSalary = salaries.FirstOrDefault(s => s.UserId == member.UserID);
                viewModel.SalaryItems.Add(new SalaryItem
                {
                    SalaryId = existingSalary?.Id ?? 0,
                    UserId = member.UserID,
                    UserName = member.Username,
                    CurrentAmount = existingSalary?.Amount ?? 0,
                    PaymentDate = existingSalary?.PaymentDate ?? DateTime.Now.AddMonths(1),
                    Status = existingSalary?.Status ?? "未設定",
                    NewAmount = existingSalary?.Amount ?? 0
                });
            }

            return View(viewModel);
        }


        // 更新薪資 - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateSalary(string laboratoryId, string userId, string userName, decimal amount)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "薪水須設定大於 0";
                return RedirectToAction("SalaryManagement", new { laboratoryId });
            }

            try
            {
                var professorId = GetCurrentProfessorId();
                _financeHandler.SetSalary(laboratoryId, userId, userName, amount, professorId);

                TempData["SuccessMessage"] = $"已成功更新 {userName} 的薪資";
                return RedirectToAction("SalaryManagement", new { laboratoryId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"更新薪資失敗: {ex.Message}";
                return RedirectToAction("SalaryManagement", new { laboratoryId });
            }
        }

        // 銀行帳戶設定 - GET
        public IActionResult BankAccountSettings(string laboratoryId)
        {
            var existingAccount = _financeHandler.GetBankAccount(laboratoryId);

            var viewModel = new BankAccountViewModel
            {
                LaboratoryId = laboratoryId
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

        // 銀行帳戶設定 - POST
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
                return RedirectToAction("Index", new { laboratoryId = model.LaboratoryId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"設定銀行帳戶失敗: {ex.Message}");
                return View(model);
            }
        }

        // 取得目前登入的教授ID (需要根據實際認證實作調整)
        private string GetCurrentProfessorId()
        {
            // 這裡需要根據實際的認證實作來取得當前教授ID
            // 例如從Session、Claims或其他認證機制取得
            return "PROF001"; // 暫時使用固定值
        }
    }
}
