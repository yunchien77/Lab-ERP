using LabERP.Models.Core;

namespace LabERP.Interface
{
    public interface IWorkSessionRepository
    {
        WorkSession Add(WorkSession workSession);
        WorkSession Update(WorkSession workSession);
        WorkSession GetById(string workSessionId);
        IEnumerable<WorkSession> GetByStudentId(string studentId);
        IEnumerable<WorkSession> GetByLabId(string labId);
        IEnumerable<WorkSession> GetByStudentAndLab(string studentId, string labId);
        WorkSession GetCurrentWorkingSession(string studentId, string labId);
        IEnumerable<WorkSession> GetByDateRange(string studentId, string labId, DateTime startDate, DateTime endDate);
        void Delete(string workSessionId);
    }

    public interface IWorkSessionHandler
    {
        WorkSession StartWork(string studentId, string labId);
        WorkSession EndWork(string studentId, string labId);
        WorkStatus GetCurrentWorkStatus(string studentId, string labId);
        IEnumerable<WorkSession> GetWorkSessions(string studentId, string labId, DateTime? startDate = null, DateTime? endDate = null);
        WorkSessionSummary GetWorkSummary(string studentId, string labId, DateTime startDate, DateTime endDate);
        IEnumerable<StudentWorkSummary> GetLabWorkSummary(string labId, DateTime startDate, DateTime endDate);
    }

    // 工作時段摘要
    public class WorkSessionSummary
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string LabId { get; set; }
        public string LabName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TotalHours { get; set; }
        public int TotalSessions { get; set; }
        public double AverageHoursPerSession { get; set; }
    }

    // 學生工作摘要
    public class StudentWorkSummary
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public double TotalHours { get; set; }
        public int TotalSessions { get; set; }
        public DateTime? LastWorkDate { get; set; }
        public bool HasWorkedThisWeek { get; set; }
    }
}