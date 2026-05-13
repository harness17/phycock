namespace Phycock.Models
{
    /// <summary>
    /// 統計ページ ViewModel。
    /// </summary>
    public class StatisticsViewModel
    {
        /// <summary>週次体調統計。</summary>
        public ChartSeriesDto WeeklyHealth { get; set; } = new();

        /// <summary>月次体調統計。</summary>
        public ChartSeriesDto MonthlyHealth { get; set; } = new();

        /// <summary>週次睡眠統計。</summary>
        public ChartSeriesDto WeeklySleep { get; set; } = new();

        /// <summary>週次通所統計。</summary>
        public AttendanceStatsDto WeeklyAttendance { get; set; } = new();
    }

    /// <summary>
    /// Chart.js に渡す系列データ。
    /// </summary>
    public class ChartSeriesDto
    {
        /// <summary>ラベル配列。</summary>
        public List<string> Labels { get; set; } = new();

        /// <summary>体調データ配列。</summary>
        public List<double?> ConditionData { get; set; } = new();

        /// <summary>気分データ配列。</summary>
        public List<double?> FeelingData { get; set; } = new();

        /// <summary>睡眠時間データ配列。</summary>
        public List<double?> SleepHoursData { get; set; } = new();
    }

    /// <summary>
    /// 通所統計データ。
    /// </summary>
    public class AttendanceStatsDto
    {
        /// <summary>ラベル配列。</summary>
        public List<string> Labels { get; set; } = new();

        /// <summary>通所・在宅利用時間データ配列。</summary>
        public List<double> AttendanceHoursData { get; set; } = new();

        /// <summary>通所率データ配列。</summary>
        public List<double?> AttendanceRateData { get; set; } = new();

        /// <summary>期間内の通所・在宅利用時間合計。</summary>
        public double TotalAttendanceHours { get; set; }

        /// <summary>期間全体の通所率。</summary>
        public double? AttendanceRate { get; set; }

        /// <summary>集計対象予定数。</summary>
        public int PlannedCount { get; set; }

        /// <summary>通所扱い件数。</summary>
        public int AttendedCount { get; set; }
    }
}
