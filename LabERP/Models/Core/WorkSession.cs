using System.ComponentModel.DataAnnotations;

namespace LabERP.Models.Core
{
    public class WorkSession
    {
        [Key]
        public string WorkSessionID { get; set; }

        [Required]
        public string StudentID { get; set; }

        [Required]
        public string LabID { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public TimeSpan? WorkDuration { get; set; }

        [Required]
        public WorkStatus Status { get; set; }

        public DateTime CreatedDate { get; set; }

        // 導航屬性
        public Student Student { get; set; }
        public Laboratory Laboratory { get; set; }

        public WorkSession()
        {
            WorkSessionID = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            Status = WorkStatus.NotStarted;
        }

        // 開始工作
        public void StartWork()
        {
            if (Status == WorkStatus.Working)
            {
                throw new InvalidOperationException("已經在工作中，無法重複開始工作");
            }

            StartTime = DateTime.Now;
            Status = WorkStatus.Working;
            EndTime = null;
            WorkDuration = null;
        }

        // 結束工作
        public void EndWork()
        {
            if (Status != WorkStatus.Working)
            {
                throw new InvalidOperationException("尚未開始工作，無法結束工作");
            }

            EndTime = DateTime.Now;
            WorkDuration = EndTime - StartTime;
            Status = WorkStatus.Completed;
        }

        // 計算工作時數（小時）
        public double GetWorkHours()
        {
            if (WorkDuration.HasValue)
            {
                return WorkDuration.Value.TotalHours;
            }

            if (Status == WorkStatus.Working)
            {
                return (DateTime.Now - StartTime).TotalHours;
            }

            return 0;
        }
    }

    public enum WorkStatus
    {
        NotStarted = 0,  // 未開始工作
        Working = 1,     // 工作中
        Completed = 2    // 已完成
    }
}