using LabERP.Models.Core;
using Microsoft.AspNetCore.Http;

namespace LabERP.Interface
{
    public interface IExpenseRequestHandler
    {
        void CreateExpenseRequest(string laboratoryId, string requesterId, string requesterName,
                                decimal amount, string invoiceNumber, string category,
                                string description, string purpose, List<IFormFile> attachments);

        void ReviewExpenseRequest(int expenseRequestId, string reviewerId, bool approved, string reviewNotes);

        bool DeleteExpenseRequest(int expenseRequestId, string requesterId);

        List<ExpenseRequest> GetExpenseRequestsByLaboratory(string laboratoryId);

        List<ExpenseRequest> GetPendingExpenseRequests(string laboratoryId);

        ExpenseRequest GetExpenseRequestWithAttachments(int id);
    }
}