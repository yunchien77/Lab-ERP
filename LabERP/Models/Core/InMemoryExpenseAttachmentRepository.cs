using LabERP.Interface;

namespace LabERP.Models.Core
{
    public class InMemoryExpenseAttachmentRepository : IExpenseAttachmentRepository
    {
        private static List<ExpenseAttachment> _attachments = new List<ExpenseAttachment>();
        private static int _nextId = 1;

        public List<ExpenseAttachment> GetByExpenseRequestId(int expenseRequestId)
        {
            return _attachments.Where(a => a.ExpenseRequestId == expenseRequestId)
                              .OrderBy(a => a.UploadedAt)
                              .ToList();
        }

        public ExpenseAttachment GetById(int id)
        {
            return _attachments.FirstOrDefault(a => a.Id == id);
        }

        public void Add(ExpenseAttachment attachment)
        {
            attachment.Id = _nextId++;
            attachment.UploadedAt = DateTime.Now;
            _attachments.Add(attachment);
        }

        public void Delete(int id)
        {
            var attachment = GetById(id);
            if (attachment != null)
            {
                _attachments.Remove(attachment);
            }
        }

        public void DeleteByExpenseRequestId(int expenseRequestId)
        {
            var attachments = GetByExpenseRequestId(expenseRequestId);
            foreach (var attachment in attachments)
            {
                _attachments.Remove(attachment);
            }
        }
    }
}
