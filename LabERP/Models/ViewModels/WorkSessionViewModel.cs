using System.ComponentModel.DataAnnotations;
using LabERP.Models.Core;
using LabERP.Interface;

namespace LabERP.Models.ViewModels
{
    // 打卡主頁面視圖模型
    public class WorkSessionViewModel
    {
        public string LabID { get; set; }
        public string LabName { get; set; }
        public string StudentID { get; set; }
        public WorkStatus CurrentStatus { get; set; }
        public DateTime? CurrentStartTime { get; set; }
        public double CurrentWorkHours { get; set; }

        public bool IsWorking => CurrentStatus == WorkStatus.Working;
        public bool CanStartWork => CurrentStatus == WorkStatus.NotStarted;
        public bool CanEndWork => CurrentStatus == WorkStatus.Working;

        public string StatusText => CurrentStatus switch
        {
            WorkStatus.NotStarted => "未開始工作",
            WorkStatus.Working => "工作中",
            WorkStatus.Completed => "已完成",
            _ => "未知狀態"
        };

        public string StatusClass => CurrentStatus switch
        {
            WorkStatus.NotStarted => "text-secondary",
            WorkStatus.Working => "text-success",
            WorkStatus.Completed => "text-info",
            _ => "text-muted"
        };
    }

    // 打卡記錄視圖模型
    public class WorkSessionRecordsViewModel
    {
        public string LabID { get; set; }
        public string LabName { get; set; }

        [Display(Name = "開始日期")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "結束日期")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public List<WorkSession> WorkSessions { get; set; } = new List<WorkSession>();
        public WorkSessionSummary Summary { get; set; }

        // 計算統計資訊
        public int TotalWorkDays => WorkSessions.Where(ws => ws.Status == WorkStatus.Completed).Count();
        public double TotalWorkHours => WorkSessions.Where(ws => ws.Status == WorkStatus.Completed).Sum(ws => ws.GetWorkHours());
        public double AverageHoursPerDay => TotalWorkDays > 0 ? TotalWorkHours / TotalWorkDays : 0;
    }

    // 實驗室出勤管理視圖模型
    public class LabAttendanceViewModel
    {
        public string LabID { get; set; }
        public string LabName { get; set; }

        [Display(Name = "開始日期")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "結束日期")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public List<StudentWorkSummary> StudentSummaries { get; set; } = new List<StudentWorkSummary>();

        // 統計資訊
        public int TotalStudents => StudentSummaries.Count;
        public int ActiveStudents => StudentSummaries.Count(s => s.TotalHours > 0);
        public int StudentsWorkedThisWeek => StudentSummaries.Count(s => s.HasWorkedThisWeek);
        public double TotalLabHours => StudentSummaries.Sum(s => s.TotalHours);
        public double AverageHoursPerStudent => TotalStudents > 0 ? TotalLabHours / TotalStudents : 0;
    }

    // 學生工作記錄詳細視圖模型
    public class StudentWorkRecordsViewModel
    {
        public string LabID { get; set; }
        public string LabName { get; set; }
        public string StudentID { get; set; }
        public string StudentName { get; set; }

        [Display(Name = "開始日期")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "結束日期")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public List<WorkSession> WorkSessions { get; set; } = new List<WorkSession>();
        public WorkSessionSummary Summary { get; set; }
    }

    // 打卡統計視圖模型
    public class WorkSessionStatsViewModel
    {
        public string Period { get; set; } // 統計期間
        public double TotalHours { get; set; } // 總工作時數
        public int TotalSessions { get; set; } // 總打卡次數
        public double AverageHoursPerDay { get; set; } // 平均每日工作時數
        public int WorkingDays { get; set; } // 工作天數
        public DateTime? LastWorkDate { get; set; } // 最後工作日期

        public List<DailyWorkSummary> DailySummaries { get; set; } = new List<DailyWorkSummary>();
    }

    // 每日工作摘要
    public class DailyWorkSummary
    {
        public DateTime Date { get; set; }
        public double TotalHours { get; set; }
        public int SessionCount { get; set; }
        public TimeSpan? FirstStartTime { get; set; }
        public TimeSpan? LastEndTime { get; set; }
    }
}