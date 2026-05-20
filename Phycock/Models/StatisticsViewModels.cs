using Phycock.Common;

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

        /// <summary>週次レポート（DBデータから生成、表示・PDF両用）。</summary>
        public WeeklyReportDto WeeklyReport { get; set; } = new();

        /// <summary>月次カレンダー用の日別集計。</summary>
        public MonthlyCalendarDto MonthlyCalendar { get; set; } = new();
    }

    /// <summary>月次カレンダー DTO。</summary>
    public class MonthlyCalendarDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<MonthlyDayCellDto> Cells { get; set; } = new();

        /// <summary>月次チャート（体調・気分・睡眠内訳）。週次レポートと同仕様。</summary>
        public WeeklyReportChartDto ReportChart { get; set; } = new();

        /// <summary>前夜の睡眠時間帯ごとの「翌日の体調平均」（帯別棒グラフ用）。</summary>
        public SleepConditionByBandDto SleepConditionByBand { get; set; } = new();
    }

    /// <summary>
    /// 前夜の睡眠時間帯ごとの「翌日の体調平均」（帯別棒グラフ用）。
    /// 前夜の睡眠を4帯（不足／やや不足／適正／過剰）に分け、各帯の翌日の体調平均を集計する。
    /// </summary>
    public class SleepConditionByBandDto
    {
        /// <summary>睡眠4帯の集計（不足→過剰の固定順）。</summary>
        public List<SleepBandStatDto> Bands { get; set; } = new();

        /// <summary>集計対象の日数（前夜睡眠＋当日体調が揃った日の合計）。</summary>
        public int TotalDayCount { get; set; }

        /// <summary>傾向を1文で示す要約。集計可能な帯が2つ未満のときは null。</summary>
        public string? LeadText { get; set; }
    }

    /// <summary>睡眠1帯の集計。</summary>
    public class SleepBandStatDto
    {
        /// <summary>睡眠レベル（帯の区分）。</summary>
        public SleepLevel Level { get; set; }

        /// <summary>帯の表示ラベル（例「不足 (〜6h)」）。</summary>
        public string Label { get; set; } = "";

        /// <summary>この帯の翌日の体調平均。該当日が無いときは null。</summary>
        public double? AverageCondition { get; set; }

        /// <summary>この帯に該当した日数。</summary>
        public int DayCount { get; set; }

        /// <summary>この帯の前夜の本睡眠平均（時間）。該当日が無いときは null。</summary>
        public double? AverageNightSleepHours { get; set; }

        /// <summary>この帯の前夜の他睡眠平均（時間）。該当日が無いときは null。</summary>
        public double? AverageOtherSleepHours { get; set; }

        /// <summary>この帯の翌日が通所日（予定に通所を含む）だった日数。</summary>
        public int AttendDayCount { get; set; }

        /// <summary>この帯の翌日が在宅日（予定が全て在宅）だった日数。</summary>
        public int RemoteDayCount { get; set; }

        /// <summary>この帯の翌日がプライベート日（予定が全てプライベート）だった日数。</summary>
        public int PrivateDayCount { get; set; }

        /// <summary>この帯の翌日が予定なし日だった日数。</summary>
        public int RestDayCount { get; set; }
    }

    /// <summary>月次カレンダーの1日セル DTO。</summary>
    public class MonthlyDayCellDto
    {
        public DateTime Date { get; set; }
        public bool InMonth { get; set; }
        public double? ConditionAvg { get; set; }
        public double? FeelingAvg { get; set; }
        public double SleepTotalHours { get; set; }
        public SleepLevel SleepLevel { get; set; }
        public string ScheduleDayClass { get; set; } = "rest";
        public string ScheduleSummary { get; set; } = "予定なし";
        public string HealthTimingSummary { get; set; } = "記録なし";
    }

    /// <summary>
    /// 週次レポート DTO。表示・PDF出力ともこれを参照する。
    /// </summary>
    public class WeeklyReportDto
    {
        /// <summary>週開始日（日曜）。</summary>
        public DateTime WeekStart { get; set; }

        /// <summary>週内7日分の集計。</summary>
        public List<DailyReportDto> Days { get; set; } = new();

        /// <summary>上部のバー+ライン用 Chart データ。</summary>
        public WeeklyReportChartDto ReportChart { get; set; } = new();

        /// <summary>タイムラインチャート用データ（時間帯ベース）。</summary>
        public WeeklyTimelineDto TimelineChart { get; set; } = new();
    }

    /// <summary>1日分の集計と詳細記録。</summary>
    public class DailyReportDto
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; } = "";   // "5/4 月" 等
        public double? ConditionAvg { get; set; }
        public double? FeelingAvg { get; set; }
        public double NightSleepHours { get; set; }
        public double OtherSleepHours { get; set; }
        public List<string> ScheduleSummaryLines { get; set; } = new();  // テーブル「通所」列（AM/PM/終日ごとに1行）
        public string ScheduleDayClass { get; set; } = "rest";  // planned/remote/rest
        public List<ScheduleStripItemDto> ScheduleStrip { get; set; } = new();
        public List<HealthRecordItemDto> HealthRecords { get; set; } = new();
        public List<SleepRecordItemDto> SleepRecords { get; set; } = new();
    }

    /// <summary>スケジュールストリップの1項目。</summary>
    public class ScheduleStripItemDto
    {
        public string SessionLabel { get; set; } = "";   // "AM 通所" "PM 在宅" "予定なし"
        public string DetailLabel { get; set; } = "";    // "通所済み / ヘルスケア"
        public string StatusClass { get; set; } = "status-none";  // status-attended 等
    }

    /// <summary>体調記録1件の表示用。</summary>
    public class HealthRecordItemDto
    {
        public string TimingLabel { get; set; } = "";   // "起床時"
        public string ConditionLabel { get; set; } = "";  // 数値文字列
        public string FeelingLabel { get; set; } = "";
        public string SymptomsLabel { get; set; } = "";  // "眠気、頭痛" or "なし"
        public string? Memo { get; set; }
    }

    /// <summary>睡眠記録1件の表示用。</summary>
    public class SleepRecordItemDto
    {
        public string TypeLabel { get; set; } = "";   // "本睡眠" / "他睡眠"
        public double Hours { get; set; }
        public string? Memo { get; set; }
    }

    /// <summary>週次レポート上部チャート（Chart.js 用）。</summary>
    public class WeeklyReportChartDto
    {
        public List<string> Labels { get; set; } = new();          // ["5/4", ...]
        public List<double?> Condition { get; set; } = new();
        public List<double?> Feeling { get; set; } = new();
        public List<double> NightSleep { get; set; } = new();
        public List<double> OtherSleep { get; set; } = new();
    }

    /// <summary>週次タイムライン（時間帯バー）データ。</summary>
    public class WeeklyTimelineDto
    {
        public List<string> Labels { get; set; } = new();          // ["5/4 (月)", ...]
        // 各日に対する [start, end] の時間帯。null は該当無し。
        public List<double?[]?> NightSleepEarly { get; set; } = new();   // 当日午前 0~起床
        public List<double?[]?> NightSleepLate { get; set; } = new();    // 当日深夜 ~24
        public List<double?[]?> OtherSleep { get; set; } = new();
        public List<double?[]?> ScheduleAm { get; set; } = new();
        public List<double?[]?> SchedulePm { get; set; } = new();
        public List<double?[]?> ScheduleAmPrivate { get; set; } = new();
        public List<double?[]?> SchedulePmPrivate { get; set; } = new();
        public List<double?[]?> ScheduleAbsent { get; set; } = new();
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
