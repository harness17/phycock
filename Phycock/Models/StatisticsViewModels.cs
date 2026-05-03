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
}
