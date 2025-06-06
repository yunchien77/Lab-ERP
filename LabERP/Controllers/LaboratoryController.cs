using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LabERP.Models.Core;
using LabERP.Models.Handlers;
using LabERP.Models.ViewModels;

namespace LabERP.Controllers
{
    [Authorize]
    public class LaboratoryController : Controller
    {
        private readonly LaboratoryHandler _laboratoryService;

        public LaboratoryController(LaboratoryHandler laboratoryService)
        {
            _laboratoryService = laboratoryService;
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
                try
                {
                    // 獲取當前用戶ID
                    var userID = User.FindFirstValue("UserID");

                    // 創建實驗室
                    var labInfo = new LaboratoryCreateDto
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Website = model.Website,
                        ContactInfo = model.ContactInfo
                    };

                    var lab = _laboratoryService.CreateLaboratory(userID, labInfo);
                    return RedirectToAction("Details", new { id = lab.LabID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }

        // 顯示實驗室詳情
        public IActionResult Details(string id)
        {
            var lab = _laboratoryService.GetLaboratoryDetails(id);
            if (lab == null)
                return NotFound();

            return View(lab);
        }

        // 處理刪除成員
        [Authorize(Roles = "Professor")]
        public IActionResult RemoveMember(string labID, string memberID)
        {
            try
            {
                // 獲取當前用戶ID
                var userID = User.FindFirstValue("UserID");

                // 刪除成員
                _laboratoryService.RemoveMember(userID, labID, memberID);
                return RedirectToAction("Details", new { id = labID });
            }
            catch (Exception)
            {
                return Forbid();
            }
        }

        // 顯示編輯實驗室頁面
        [Authorize(Roles = "Professor")]
        public IActionResult Edit(string id)
        {
            var lab = _laboratoryService.GetLaboratoryDetails(id);
            if (lab == null)
                return NotFound();

            // 檢查是否是實驗室的創建者
            var userID = User.FindFirstValue("UserID");
            if (lab.Creator.UserID != userID)
                return Forbid();

            var model = new EditLabViewModel
            {
                LabID = lab.LabID,
                Name = lab.Name,
                Description = lab.Description,
                Website = lab.Website,
                ContactInfo = lab.ContactInfo
            };

            return View(model);
        }

        // 處理編輯實驗室
        [HttpPost]
        [Authorize(Roles = "Professor")]
        public IActionResult Edit(EditLabViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 獲取當前用戶ID
                    var userID = User.FindFirstValue("UserID");

                    // 更新實驗室
                    var labInfo = new LaboratoryUpdateDto
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Website = model.Website,
                        ContactInfo = model.ContactInfo
                    };

                    _laboratoryService.UpdateLaboratory(userID, model.LabID, labInfo);
                    return RedirectToAction("Details", new { id = model.LabID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
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
                try
                {
                    // 獲取當前用戶ID
                    var userID = User.FindFirstValue("UserID");

                    // 創建成員
                    var memberInfo = new MemberCreateDto
                    {
                        Username = model.Username,
                        Email = model.Email
                    };

                    _laboratoryService.AddMember(userID, model.LabID, memberInfo);
                    return RedirectToAction("Details", new { id = model.LabID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(model);
        }
    }
}
