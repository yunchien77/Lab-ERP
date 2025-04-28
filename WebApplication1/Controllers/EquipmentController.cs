using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Interface;
using WebApplication1.Models.Core;
using WebApplication1.Models.Handlers;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class EquipmentController : Controller
    {
        private readonly EquipmentHandler _equipmentHandler;
        private readonly ILaboratoryRepository _laboratoryRepository; // 新增

        public EquipmentController(
            EquipmentHandler equipmentHandler,
            ILaboratoryRepository laboratoryRepository) // 新增參數
        {
            _equipmentHandler = equipmentHandler;
            _laboratoryRepository = laboratoryRepository; // 新增
        }

        /// 顯示實驗室設備列表
        public IActionResult Index(string labID)
        {
            if (string.IsNullOrEmpty(labID))
            {
                TempData["ErrorMessage"] = "實驗室ID不能為空";
                return RedirectToAction("Dashboard", "User");
            }

            var laboratory = _laboratoryRepository.GetById(labID);
            if (laboratory == null)
            {
                TempData["ErrorMessage"] = "實驗室不存在";
                return RedirectToAction("Dashboard", "User");
            }

            var equipments = _equipmentHandler.GetEquipmentsByLab(labID);

            ViewBag.Laboratory = laboratory; // 設置完整的Laboratory對象
            ViewBag.LaboratoryID = labID;    // 保留原有的ID設置

            return View(equipments);
        }

        // 顯示新增設備頁面
        [Authorize(Roles = "Professor")]
        public IActionResult Create(string labID)
        {
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
                try
                {
                    var userID = User.FindFirstValue("UserID");
                    var equipmentInfo = new CreateEquipmentDto
                    {
                        Name = model.Name,
                        Description = model.Description,
                        TotalQuantity = model.TotalQuantity,
                        PurchaseDate = model.PurchaseDate,
                        LaboratoryID = model.LaboratoryID
                    };

                    _equipmentHandler.CreateEquipment(userID, equipmentInfo);
                    return RedirectToAction("Index", new { labID = model.LaboratoryID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        // 處理刪除設備
        [HttpPost]
        [Authorize(Roles = "Professor")]
        public IActionResult Delete(string id)
        {
            try
            {
                var userID = User.FindFirstValue("UserID");

                // 在刪除前獲取設備以取得laboratoryID
                var equipment = _equipmentHandler.GetEquipmentById(id);
                if (equipment == null)
                {
                    TempData["ErrorMessage"] = "設備不存在";
                    return RedirectToAction("Dashboard", "User");
                }

                string labID = equipment.LaboratoryID; // 獲取設備所屬的實驗室ID

                _equipmentHandler.DeleteEquipment(userID, id);

                // 刪除後重定向回實驗室的設備列表
                return RedirectToAction("Index", new { labID = labID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Dashboard", "User");
            }
        }

        // 顯示借用設備頁面
        [Authorize(Roles = "Student")]
        public IActionResult Borrow(string id)
        {
            var equipment = _equipmentHandler.GetEquipmentById(id);
            if (equipment == null)
            {
                TempData["ErrorMessage"] = "設備不存在";
                return RedirectToAction("Dashboard", "User");
            }

            if (equipment.AvailableQuantity <= 0)
            {
                TempData["ErrorMessage"] = "該設備當前沒有可用數量";
                return RedirectToAction("Index", new { labID = equipment.LaboratoryID });
            }

            var model = new BorrowEquipmentViewModel
            {
                EquipmentID = id
            };

            ViewBag.Equipment = equipment;

            return View(model);
        }

        // 處理借用設備
        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult Borrow(BorrowEquipmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userID = User.FindFirstValue("UserID");
                    var borrowInfo = new BorrowEquipmentDto
                    {
                        EquipmentID = model.EquipmentID,
                        Quantity = model.Quantity,
                        Notes = model.Notes
                    };

                    _equipmentHandler.BorrowEquipment(userID, borrowInfo);
                    return RedirectToAction("MyEquipments");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // 如果模型無效或發生異常，需要重新設置ViewBag.Equipment
            var equipment = _equipmentHandler.GetEquipmentById(model.EquipmentID);
            ViewBag.Equipment = equipment;

            return View(model);
        }

        // 顯示我的借用設備列表
        [Authorize(Roles = "Student")]
        public IActionResult MyEquipments()
        {
            var userID = User.FindFirstValue("UserID");
            var records = _equipmentHandler.GetStudentBorrowRecords(userID);
            return View(records);
        }

        // 歸還設備
        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult Return(string id)
        {
            try
            {
                var userID = User.FindFirstValue("UserID");
                _equipmentHandler.ReturnEquipment(userID, id);
                return RedirectToAction("MyEquipments");
            }
            catch (Exception)
            {
                return Forbid();
            }
        }
    }
}