using Phycock.Entity.Enums;

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

        /// <summary>今日の睡眠記録があるかどうか。</summary>
        public bool HasSleepRecord { get; set; }

        /// <summary>直近の体調レベル（今日の最新記録）。記録なしは null。</summary>
        public ConditionLevel? LatestCondition { get; set; }

        /// <summary>直近の気分レベル（今日の最新記録）。記録なしは null。</summary>
        public FeelingLevel? LatestFeeling { get; set; }

        /// <summary>データ取得に失敗したかどうか。true の場合チェックリストは unavailable 表示。</summary>
        public bool IsUnavailable { get; set; }
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
