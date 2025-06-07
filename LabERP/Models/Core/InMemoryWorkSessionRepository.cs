using LabERP.Interface;
using LabERP.Models.Core;

namespace LabERP.Models.Core
{
    public class InMemoryWorkSessionRepository : IWorkSessionRepository
    {
        private readonly List<WorkSession> _workSessions = new List<WorkSession>();

        public WorkSession Add(WorkSession workSession)
        {
            _workSessions.Add(workSession);
            return workSession;
        }

        public WorkSession Update(WorkSession workSession)
        {
            var existing = _workSessions.FirstOrDefault(ws => ws.WorkSessionID == workSession.WorkSessionID);
            if (existing != null)
            {
                var index = _workSessions.IndexOf(existing);
                _workSessions[index] = workSession;
            }
            return workSession;
        }

        public WorkSession GetById(string workSessionId)
        {
            return _workSessions.FirstOrDefault(ws => ws.WorkSessionID == workSessionId);
        }

        public IEnumerable<WorkSession> GetByStudentId(string studentId)
        {
            return _workSessions.Where(ws => ws.StudentID == studentId);
        }

        public IEnumerable<WorkSession> GetByLabId(string labId)
        {
            return _workSessions.Where(ws => ws.LabID == labId);
        }

        public IEnumerable<WorkSession> GetByStudentAndLab(string studentId, string labId)
        {
            return _workSessions.Where(ws => ws.StudentID == studentId && ws.LabID == labId);
        }

        public WorkSession GetCurrentWorkingSession(string studentId, string labId)
        {
            return _workSessions.FirstOrDefault(ws =>
                ws.StudentID == studentId &&
                ws.LabID == labId &&
                ws.Status == WorkStatus.Working);
        }

        public IEnumerable<WorkSession> GetByDateRange(string studentId, string labId, DateTime startDate, DateTime endDate)
        {
            return _workSessions.Where(ws =>
                ws.StudentID == studentId &&
                ws.LabID == labId &&
                ws.StartTime >= startDate &&
                ws.StartTime <= endDate);
        }

        public void Delete(string workSessionId)
        {
            var workSession = _workSessions.FirstOrDefault(ws => ws.WorkSessionID == workSessionId);
            if (workSession != null)
            {
                _workSessions.Remove(workSession);
            }
        }
    }
}