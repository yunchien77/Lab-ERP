using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Models.Core;
using WebApplication1.Models.ViewModels;



namespace WebApplication1.Controllers
{
    [Authorize]
    public class EquipmentController : Controller
    {
        private AccountController _accountController;
        private static List<Equipment> _equipments = new List<Equipment>();

        public EquipmentController(AccountController accountController)
        {
            _accountController = accountController;
        }

        // 顯示實驗室設備列表
        public IActionResult Index(string labID)
        {
            var lab = LaboratoryController._laboratories.Find(l => l.LabID == labID);
            if (lab == null)
                return NotFound();

            ViewBag.Laboratory = lab;
            var equipments = _equipments.Where(e => e.LaboratoryID == labID).ToList();
            return View(equipments);
        }

        // 顯示新增設備頁面
        [Authorize(Roles = "Professor")]
        public IActionResult Create(string labID)
        {
            var lab = LaboratoryController._laboratories.Find(l => l.LabID == labID);
            if (lab == null)
                return NotFound();

            // 檢查是否是實驗室的創建者
            var userID = User.FindFirstValue("UserID");
            if (lab.Creator.UserID != userID)
                return Forbid();

            var model = new CreateEquipmentViewModel
            {
                LaboratoryID = labID,
                PurchaseDate = DateTime.Now
            };

            return View(model);
        }

        // 處理新增設備
        [HttpPost]
        [Authorize(Roles = "Professor")]
        public IActionResult Create(CreateEquipmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var lab = LaboratoryController._laboratories.Find(l => l.LabID == model.LaboratoryID);
                if (lab == null)
                    return NotFound();

                // 檢查是否是實驗室的創建者
                var userID = User.FindFirstValue("UserID");
                if (lab.Creator.UserID != userID)
                    return Forbid();

                var equipment = new Equipment
                {
                    Name = model.Name,
                    Description = model.Description,
                    TotalQuantity = model.TotalQuantity,
                    AvailableQuantity = model.TotalQuantity,
                    PurchaseDate = model.PurchaseDate,
                    LaboratoryID = model.LaboratoryID
                };

                _equipments.Add(equipment);

                return RedirectToAction("Index", new { labID = model.LaboratoryID });
            }

            return View(model);
        }

        // 處理刪除設備
        [HttpPost]
        [Authorize(Roles = "Professor")]
        public IActionResult Delete(string id)
        {
            var equipment = _equipments.Find(e => e.EquipmentID == id);
            if (equipment == null)
                return NotFound();

            var lab = LaboratoryController._laboratories.Find(l => l.LabID == equipment.LaboratoryID);
            if (lab == null)
                return NotFound();

            // 檢查是否是實驗室的創建者
            var userID = User.FindFirstValue("UserID");
            if (lab.Creator.UserID != userID)
                return Forbid();

            _equipments.Remove(equipment);

            return RedirectToAction("Index", new { labID = equipment.LaboratoryID });
        }

        // 顯示借用設備頁面
        [Authorize(Roles = "Student")]
        public IActionResult Borrow(string id)
        {
            var equipment = _equipments.Find(e => e.EquipmentID == id);
            if (equipment == null)
                return NotFound();

            if (equipment.AvailableQuantity <= 0)
            {
                TempData["ErrorMessage"] = "該設備已無可借用數量";
                return RedirectToAction("Index", new { labID = equipment.LaboratoryID });
            }

            ViewBag.Equipment = equipment;
            var model = new BorrowEquipmentViewModel
            {
                EquipmentID = id
            };

            return View(model);
        }

        // 處理借用設備
        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult Borrow(BorrowEquipmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var equipment = _equipments.Find(e => e.EquipmentID == model.EquipmentID);
                if (equipment == null)
                    return NotFound();

                if (equipment.AvailableQuantity < model.Quantity)
                {
                    ModelState.AddModelError("", "借用數量超過可用數量");
                    ViewBag.Equipment = equipment;
                    return View(model);
                }

                // 獲取當前用戶ID
                var userID = User.FindFirstValue("UserID");
                var student = _accountController.GetUser(userID) as Student;

                if (student == null)
                    return NotFound();

                // 創建借用記錄
                var record = new BorrowRecord
                {
                    StudentID = student.UserID,
                    Student = student,
                    EquipmentID = equipment.EquipmentID,
                    Equipment = equipment,
                    Quantity = model.Quantity,
                    Notes = model.Notes
                };

                // 更新設備可用數量
                equipment.AvailableQuantity -= model.Quantity;
                equipment.BorrowRecords.Add(record);
                student.BorrowRecords.Add(record);

                return RedirectToAction("MyEquipments");
            }

            var equipmentForView = _equipments.Find(e => e.EquipmentID == model.EquipmentID);
            ViewBag.Equipment = equipmentForView;
            return View(model);
        }

        // 顯示我的借用設備列表
        [Authorize(Roles = "Student")]
        public IActionResult MyEquipments()
        {
            // 獲取當前用戶ID
            var userID = User.FindFirstValue("UserID");
            var student = _accountController.GetUser(userID) as Student;

            if (student == null)
                return NotFound();

            // 獲取學生的借用記錄
            var records = student.BorrowRecords.Where(r => r.Status == "Borrowed").ToList();

            return View(records);
        }

        // 歸還設備
        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult Return(string id)
        {
            // 獲取當前用戶ID
            var userID = User.FindFirstValue("UserID");
            var student = _accountController.GetUser(userID) as Student;

            if (student == null)
                return NotFound();

            // 查找借用記錄
            var record = student.BorrowRecords.Find(r => r.RecordID == id && r.Status == "Borrowed");
            if (record == null)
                return NotFound();

            // 更新記錄狀態
            record.Status = "Returned";
            record.ReturnDate = DateTime.Now;

            // 更新設備可用數量
            var equipment = _equipments.Find(e => e.EquipmentID == record.EquipmentID);
            if (equipment != null)
            {
                equipment.AvailableQuantity += record.Quantity;
            }

            return RedirectToAction("MyEquipments");
        }
    }
}