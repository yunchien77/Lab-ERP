using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemoryExpenseRequestRepository : IExpenseRequestRepository
    {
        private static List<ExpenseRequest> _expenseRequests = new List<ExpenseRequest>();
        private static int _nextId = 1;

        public List<ExpenseRequest> GetByLaboratoryId(string laboratoryId)
        {
            return _expenseRequests.Where(e => e.LaboratoryId == laboratoryId)
                                  .OrderByDescending(e => e.CreatedAt)
                                  .ToList();
        }

        public List<ExpenseRequest> GetByRequesterId(string requesterId)
        {
            return _expenseRequests.Where(e => e.RequesterId == requesterId)
                                  .OrderByDescending(e => e.CreatedAt)
                                  .ToList();
        }

        public List<ExpenseRequest> GetByStatus(string laboratoryId, ExpenseRequestStatus status)
        {
            return _expenseRequests.Where(e => e.LaboratoryId == laboratoryId && e.Status == status)
                                  .OrderByDescending(e => e.CreatedAt)
                                  .ToList();
        }

        public ExpenseRequest GetById(int id)
        {
            return _expenseRequests.FirstOrDefault(e => e.Id == id);
        }

        public void Add(ExpenseRequest expenseRequest)
        {
            expenseRequest.Id = _nextId++;
            expenseRequest.CreatedAt = DateTime.Now;
            expenseRequest.UpdatedAt = DateTime.Now;
            _expenseRequests.Add(expenseRequest);
        }

        public void Update(ExpenseRequest expenseRequest)
        {
            var existing = GetById(expenseRequest.Id);
            if (existing != null)
            {
                existing.Amount = expenseRequest.Amount;
                existing.InvoiceNumber = expenseRequest.InvoiceNumber;
                existing.Category = expenseRequest.Category;
                existing.Description = expenseRequest.Description;
                existing.Purpose = expenseRequest.Purpose;
                existing.Status = expenseRequest.Status;
                existing.ReviewDate = expenseRequest.ReviewDate;
                existing.ReviewedBy = expenseRequest.ReviewedBy;
                existing.ReviewNotes = expenseRequest.ReviewNotes;
                existing.UpdatedAt = DateTime.Now;
            }
        }

        public void Delete(int id)
        {
            var expenseRequest = GetById(id);
            if (expenseRequest != null)
            {
                _expenseRequests.Remove(expenseRequest);
            }
        }

        public bool CanDelete(int id, string requesterId)
        {
            var expenseRequest = GetById(id);
            return expenseRequest != null &&
                   expenseRequest.RequesterId == requesterId &&
                   expenseRequest.Status == ExpenseRequestStatus.Pending;
        }
    }
}
