using LabERP.Interface;
using LabERP.Models.Core;

namespace LabERP.Models.Handlers
{
    public class ExpenseRequestHandler
    {
        private readonly IExpenseRequestRepository _expenseRequestRepository;
        private readonly IExpenseAttachmentRepository _expenseAttachmentRepository;
        private readonly IFinanceRepository _financeRepository;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ILaboratoryRepository _laboratoryRepository;

        public ExpenseRequestHandler(
            IExpenseRequestRepository expenseRequestRepository,
            IExpenseAttachmentRepository expenseAttachmentRepository,
            IFinanceRepository financeRepository,
            INotificationService notificationService,
            IUserRepository userRepository,
            ILaboratoryRepository laboratoryRepository)
        {
            _expenseRequestRepository = expenseRequestRepository;
            _expenseAttachmentRepository = expenseAttachmentRepository;
            _financeRepository = financeRepository;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _laboratoryRepository = laboratoryRepository;
        }

        public void CreateExpenseRequest(string laboratoryId, string requesterId, string requesterName,
                                       decimal amount, string invoiceNumber, string category,
                                       string description, string purpose, List<IFormFile> attachments)
        {
            var expenseRequest = new ExpenseRequest
            {
                LaboratoryId = laboratoryId,
                RequesterId = requesterId,
                RequesterName = requesterName,
                Amount = amount,
                InvoiceNumber = invoiceNumber,
                Category = category,
                Description = description,
                Purpose = purpose
            };

            _expenseRequestRepository.Add(expenseRequest);

            // 處理附件上傳
            if (attachments != null && attachments.Any())
            {
                ProcessAttachments(expenseRequest.Id, attachments);
            }

            // 通知教授
            NotifyProfessorNewExpenseRequest(laboratoryId, expenseRequest);

            Console.WriteLine($"新報帳申請已提交 - 申請人: {requesterName}, 金額: NT${amount:N0}");
        }

        public void ReviewExpenseRequest(int expenseRequestId, string reviewerId, bool approved, string reviewNotes)
        {
            var expenseRequest = _expenseRequestRepository.GetById(expenseRequestId);
            if (expenseRequest == null)
            {
                throw new ArgumentException("找不到指定的報帳申請");
            }

            if (expenseRequest.Status != ExpenseRequestStatus.Pending)
            {
                throw new InvalidOperationException("只能審核狀態為未審核的報帳申請");
            }

            // 檢查可用預算
            if (approved)
            {
                var availableBudget = _financeRepository.GetBalance(expenseRequest.LaboratoryId);
                if (expenseRequest.Amount > availableBudget)
                {
                    // 預算不足，取消通過並通知教授
                    NotifyProfessorInsufficientBudget(expenseRequest, availableBudget);
                    throw new InvalidOperationException($"預算不足！可用預算: NT${availableBudget:N0}，申請金額: NT${expenseRequest.Amount:N0}");
                }

                // 通過審核，更新財務記錄
                expenseRequest.Status = ExpenseRequestStatus.Approved;
                AddExpenseToFinanceRecord(expenseRequest, reviewerId);
            }
            else
            {
                // 拒絕申請
                expenseRequest.Status = ExpenseRequestStatus.Rejected;
            }

            expenseRequest.ReviewDate = DateTime.Now;
            expenseRequest.ReviewedBy = reviewerId;
            expenseRequest.ReviewNotes = reviewNotes;

            _expenseRequestRepository.Update(expenseRequest);

            // 通知申請人
            NotifyRequesterReviewResult(expenseRequest);

            Console.WriteLine($"報帳審核完成 - 申請人: {expenseRequest.RequesterName}, 結果: {(approved ? "通過" : "拒絕")}");
        }

        public bool DeleteExpenseRequest(int expenseRequestId, string requesterId)
        {
            if (!_expenseRequestRepository.CanDelete(expenseRequestId, requesterId))
            {
                return false;
            }

            var expenseRequest = _expenseRequestRepository.GetById(expenseRequestId);
            if (expenseRequest != null)
            {
                // 刪除相關附件檔案
                DeleteAttachmentFiles(expenseRequestId);

                // 刪除附件記錄
                _expenseAttachmentRepository.DeleteByExpenseRequestId(expenseRequestId);

                // 刪除報帳申請
                _expenseRequestRepository.Delete(expenseRequestId);

                Console.WriteLine($"報帳申請已刪除 - ID: {expenseRequestId}, 申請人: {expenseRequest.RequesterName}");
                return true;
            }

            return false;
        }

        public List<ExpenseRequest> GetExpenseRequestsByLaboratory(string laboratoryId)
        {
            var requests = _expenseRequestRepository.GetByLaboratoryId(laboratoryId);

            // 載入附件資訊
            foreach (var request in requests)
            {
                request.Attachments = _expenseAttachmentRepository.GetByExpenseRequestId(request.Id);
            }

            return requests;
        }

        public List<ExpenseRequest> GetPendingExpenseRequests(string laboratoryId)
        {
            return _expenseRequestRepository.GetByStatus(laboratoryId, ExpenseRequestStatus.Pending);
        }

        public ExpenseRequest GetExpenseRequestWithAttachments(int id)
        {
            var request = _expenseRequestRepository.GetById(id);
            if (request != null)
            {
                request.Attachments = _expenseAttachmentRepository.GetByExpenseRequestId(id);
            }
            return request;
        }

        private void ProcessAttachments(int expenseRequestId, List<IFormFile> attachments)
        {
            var uploadPath = Path.Combine("wwwroot", "uploads", "expense-attachments");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            foreach (var file in attachments)
            {
                if (IsValidFile(file))
                {
                    var fileName = $"{expenseRequestId}_{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    var attachment = new ExpenseAttachment
                    {
                        ExpenseRequestId = expenseRequestId,
                        FileName = file.FileName,
                        FilePath = $"/uploads/expense-attachments/{fileName}",
                        FileType = file.ContentType,
                        FileSize = file.Length
                    };

                    _expenseAttachmentRepository.Add(attachment);
                }
            }
        }

        private bool IsValidFile(IFormFile file)
        {
            // 檢查檔案大小 (5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return false;
            }

            // 檢查檔案類型
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "application/pdf" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }

        private void DeleteAttachmentFiles(int expenseRequestId)
        {
            var attachments = _expenseAttachmentRepository.GetByExpenseRequestId(expenseRequestId);
            foreach (var attachment in attachments)
            {
                var fullPath = Path.Combine("wwwroot", attachment.FilePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刪除附件檔案失敗: {fullPath}, 錯誤: {ex.Message}");
                    }
                }
            }
        }

        private void AddExpenseToFinanceRecord(ExpenseRequest expenseRequest, string reviewerId)
        {
            var financeRecord = new FinanceRecord
            {
                LaboratoryId = expenseRequest.LaboratoryId,
                Type = "Expense",
                Amount = expenseRequest.Amount,
                Description = $"報帳支出 - {expenseRequest.RequesterName} ({expenseRequest.Category})",
                Category = "Expense_Reimbursement",
                CreatedBy = reviewerId,
                CreatedAt = DateTime.Now
            };

            _financeRepository.Add(financeRecord);
        }

        private void NotifyProfessorNewExpenseRequest(string laboratoryId, ExpenseRequest expenseRequest)
        {
            var laboratory = _laboratoryRepository.GetById(laboratoryId);
            if (laboratory?.Creator != null)
            {
                Console.WriteLine($"[報帳通知] 教授 {laboratory.Creator.Username} ({laboratory.Creator.Email}):");
                Console.WriteLine($"實驗室 '{laboratory.Name}' 有新的報帳申請");
                Console.WriteLine($"申請人: {expenseRequest.RequesterName}");
                Console.WriteLine($"金額: NT${expenseRequest.Amount:N0}");
                Console.WriteLine($"類別: {expenseRequest.Category}");
                Console.WriteLine($"描述: {expenseRequest.Description}");
            }
        }

        private void NotifyRequesterReviewResult(ExpenseRequest expenseRequest)
        {
            var requester = _userRepository.GetUserById(expenseRequest.RequesterId);
            if (requester != null)
            {
                var statusText = expenseRequest.Status == ExpenseRequestStatus.Approved ? "通過" : "不通過";
                Console.WriteLine($"[報帳結果通知] 學生 {requester.Username} ({requester.Email}):");
                Console.WriteLine($"您的報帳申請審核結果: {statusText}");
                Console.WriteLine($"申請金額: NT${expenseRequest.Amount:N0}");
                Console.WriteLine($"審核日期: {expenseRequest.ReviewDate:yyyy-MM-dd HH:mm}");
                if (!string.IsNullOrEmpty(expenseRequest.ReviewNotes))
                {
                    Console.WriteLine($"審核備註: {expenseRequest.ReviewNotes}");
                }
            }
        }

        private void NotifyProfessorInsufficientBudget(ExpenseRequest expenseRequest, decimal availableBudget)
        {
            var laboratory = _laboratoryRepository.GetById(expenseRequest.LaboratoryId);
            if (laboratory?.Creator != null)
            {
                Console.WriteLine($"[預算不足通知] 教授 {laboratory.Creator.Username} ({laboratory.Creator.Email}):");
                Console.WriteLine($"報帳申請因預算不足無法通過");
                Console.WriteLine($"申請人: {expenseRequest.RequesterName}");
                Console.WriteLine($"申請金額: NT${expenseRequest.Amount:N0}");
                Console.WriteLine($"可用預算: NT${availableBudget:N0}");
            }
        }
    }
}
