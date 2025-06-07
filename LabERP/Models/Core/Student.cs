namespace LabERP.Models.Core
{
    public class Student : User
    {
        public string StudentID { get; set; }
        public string PhoneNumber { get; set; }
        public List<BorrowRecord> BorrowRecords { get; set; }
        public List<WorkSession> WorkSessions { get; set; }

        public Student()
        {
            Role = "Student";
            BorrowRecords = new List<BorrowRecord>();
            WorkSessions = new List<WorkSession>();
        }

        // 獲取當前工作狀態
        public WorkStatus GetCurrentWorkStatus(string labID)
        {
            var currentSession = WorkSessions
                .Where(ws => ws.LabID == labID && ws.Status == WorkStatus.Working)
                .OrderByDescending(ws => ws.StartTime)
                .FirstOrDefault();

            return currentSession?.Status ?? WorkStatus.NotStarted;
        }

        // 獲取當前正在進行的工作時段
        public WorkSession GetCurrentWorkSession(string labID)
        {
            return WorkSessions
                .Where(ws => ws.LabID == labID && ws.Status == WorkStatus.Working)
                .OrderByDescending(ws => ws.StartTime)
                .FirstOrDefault();
        }

        // 獲取特定實驗室的工作時段記錄
        public IEnumerable<WorkSession> GetWorkSessionsByLab(string labID)
        {
            return WorkSessions
                .Where(ws => ws.LabID == labID)
                .OrderByDescending(ws => ws.StartTime);
        }

        // 計算特定期間的總工作時數
        public double GetTotalWorkHours(string labID, DateTime? startDate = null, DateTime? endDate = null)
        {
            var sessions = WorkSessions
                .Where(ws => ws.LabID == labID && ws.Status == WorkStatus.Completed);

            if (startDate.HasValue)
            {
                sessions = sessions.Where(ws => ws.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                sessions = sessions.Where(ws => ws.StartTime <= endDate.Value);
            }

            return sessions.Sum(ws => ws.GetWorkHours());
        }
    }
}