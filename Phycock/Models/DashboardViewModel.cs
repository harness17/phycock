namespace Phycock.Models
{
    /// <summary>
    /// ダッシュボード表示 ViewModel。
    /// </summary>
    public class DashboardViewModel
    {
        /// <summary>今日の通所予定。</summary>
        public List<ScheduleEntryDetailDto> TodayScheduleEntries { get; set; } = new();

        /// <summary>今日の体調記録。</summary>
        public List<HealthRecordListViewModel> TodayHealthRecords { get; set; } = new();

        /// <summary>直近7日分の体調・睡眠サマリー。</summary>
        public WeeklySummaryDto WeeklySummary { get; set; } = new();
    }

    /// <summary>
    /// 直近7日分の体調・睡眠サマリー DTO。
    /// </summary>
    public class WeeklySummaryDto
    {
        /// <summary>集計開始日。</summary>
        public DateTime StartDate { get; set; }

        /// <summary>集計終了日。</summary>
        public DateTime EndDate { get; set; }

        /// <summary>体調平均。</summary>
        public double? AverageCondition { get; set; }

        /// <summary>気分平均。</summary>
        public double? AverageFeeling { get; set; }

        /// <summary>睡眠時間合計。</summary>
        public TimeSpan TotalSleepDuration { get; set; }
    }
}
