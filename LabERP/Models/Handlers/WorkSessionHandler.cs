using LabERP.Interface;
using LabERP.Models.Core;

namespace LabERP.Models.Handlers
{
    public class WorkSessionHandler : IWorkSessionHandler
    {
        private readonly IWorkSessionRepository _workSessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILaboratoryRepository _laboratoryRepository;

        public WorkSessionHandler(
            IWorkSessionRepository workSessionRepository,
            IUserRepository userRepository,
            ILaboratoryRepository laboratoryRepository)
        {
            _workSessionRepository = workSessionRepository;
            _userRepository = userRepository;
            _laboratoryRepository = laboratoryRepository;
        }

        public WorkSession StartWork(string studentId, string labId)
        {
            // 驗證學生和實驗室
            var student = _userRepository.GetUserById(studentId) as Student;
            var laboratory = _laboratoryRepository.GetById(labId);

            if (student == null)
            {
                throw new InvalidOperationException("學生不存在");
            }

            if (laboratory == null)
            {
                throw new InvalidOperationException("實驗室不存在");
            }

            // 檢查學生是否為實驗室成員
            if (!laboratory.Members.Any(m => m.UserID == studentId))
            {
                throw new InvalidOperationException("您不是此實驗室的成員");
            }

            // 檢查是否已有正在進行的工作時段
            var currentSession = _workSessionRepository.GetCurrentWorkingSession(studentId, labId);
            if (currentSession != null)
            {
                throw new InvalidOperationException("您已經在工作中，無法重複開始工作");
            }

            // 創建新的工作時段
            var workSession = new WorkSession
            {
                StudentID = studentId,
                LabID = labId,
                Student = student,
                Laboratory = laboratory
            };

            workSession.StartWork();

            // 保存工作時段
            _workSessionRepository.Add(workSession);

            return workSession;
        }

        public WorkSession EndWork(string studentId, string labId)
        {
            // 獲取當前正在進行的工作時段
            var currentSession = _workSessionRepository.GetCurrentWorkingSession(studentId, labId);

            if (currentSession == null)
            {
                throw new InvalidOperationException("您尚未開始工作，無法結束工作");
            }

            // 結束工作
            currentSession.EndWork();

            // 更新工作時段
            _workSessionRepository.Update(currentSession);

            return currentSession;
        }

        public WorkStatus GetCurrentWorkStatus(string studentId, string labId)
        {
            var currentSession = _workSessionRepository.GetCurrentWorkingSession(studentId, labId);
            return currentSession?.Status ?? WorkStatus.NotStarted;
        }

        public IEnumerable<WorkSession> GetWorkSessions(string studentId, string labId, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                return _workSessionRepository.GetByDateRange(studentId, labId, startDate.Value, endDate.Value);
            }

            return _workSessionRepository.GetByStudentAndLab(studentId, labId);
        }

        public WorkSessionSummary GetWorkSummary(string studentId, string labId, DateTime startDate, DateTime endDate)
        {
            var sessions = _workSessionRepository.GetByDateRange(studentId, labId, startDate, endDate)
                .Where(ws => ws.Status == WorkStatus.Completed);

            var student = _userRepository.GetUserById(studentId);
            var laboratory = _laboratoryRepository.GetById(labId);

            var totalHours = sessions.Sum(ws => ws.GetWorkHours());
            var totalSessions = sessions.Count();

            return new WorkSessionSummary
            {
                StudentId = studentId,
                StudentName = student?.Username ?? "未知",
                LabId = labId,
                LabName = laboratory?.Name ?? "未知",
                StartDate = startDate,
                EndDate = endDate,
                TotalHours = totalHours,
                TotalSessions = totalSessions,
                AverageHoursPerSession = totalSessions > 0 ? totalHours / totalSessions : 0
            };
        }

        public IEnumerable<StudentWorkSummary> GetLabWorkSummary(string labId, DateTime startDate, DateTime endDate)
        {
            var laboratory = _laboratoryRepository.GetById(labId);
            if (laboratory == null)
            {
                throw new InvalidOperationException("實驗室不存在");
            }

            var summaries = new List<StudentWorkSummary>();
            var weekStart = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);

            foreach (var member in laboratory.Members.OfType<Student>())
            {
                var sessions = _workSessionRepository.GetByDateRange(member.UserID, labId, startDate, endDate)
                    .Where(ws => ws.Status == WorkStatus.Completed);

                var weekSessions = _workSessionRepository.GetByDateRange(member.UserID, labId, weekStart, DateTime.Now);

                var totalHours = sessions.Sum(ws => ws.GetWorkHours());
                var totalSessions = sessions.Count();
                var lastWorkDate = sessions.OrderByDescending(ws => ws.StartTime).FirstOrDefault()?.StartTime;
                var hasWorkedThisWeek = weekSessions.Any();

                summaries.Add(new StudentWorkSummary
                {
                    StudentId = member.UserID,
                    StudentName = member.Username,
                    TotalHours = totalHours,
                    TotalSessions = totalSessions,
                    LastWorkDate = lastWorkDate,
                    HasWorkedThisWeek = hasWorkedThisWeek
                });
            }

            return summaries;
        }
    }
}