using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IExpenseRequestRepository
    {
        List<ExpenseRequest> GetByLaboratoryId(string laboratoryId);
        List<ExpenseRequest> GetByRequesterId(string requesterId);
        List<ExpenseRequest> GetByStatus(string laboratoryId, ExpenseRequestStatus status);
        ExpenseRequest GetById(int id);
        void Add(ExpenseRequest expenseRequest);
        void Update(ExpenseRequest expenseRequest);
        void Delete(int id);
        bool CanDelete(int id, string requesterId); // 檢查是否能刪除（未審核且為申請人）
    }
}
