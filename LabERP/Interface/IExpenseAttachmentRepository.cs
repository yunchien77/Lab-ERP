using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IExpenseAttachmentRepository
    {
        List<ExpenseAttachment> GetByExpenseRequestId(int expenseRequestId);
        ExpenseAttachment GetById(int id);
        void Add(ExpenseAttachment attachment);
        void Delete(int id);
        void DeleteByExpenseRequestId(int expenseRequestId);
    }
}
