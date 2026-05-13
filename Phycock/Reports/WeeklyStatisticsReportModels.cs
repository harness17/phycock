namespace Phycock.Reports
{
    /// <summary>
    /// 週次統計PDFの表示データ。
    /// </summary>
    public class WeeklyStatisticsReportModel
    {
        public string Title { get; set; } = "週次統計レポート";

        public string TargetUserName { get; set; } = "";

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public List<WeeklyStatisticsDayReportModel> Days { get; set; } = new();
    }

    /// <summary>
    /// 週次統計PDFの日別表示データ。
    /// </summary>
    public class WeeklyStatisticsDayReportModel
    {
        public DateOnly Date { get; set; }

        public double? ConditionAverage { get; set; }

        public double? FeelingAverage { get; set; }

        public double NightSleepHours { get; set; }

        public double OtherSleepHours { get; set; }

        public List<ScheduleReportEntryModel> Schedules { get; set; } = new();

        public List<HealthRecordReportEntryModel> HealthRecords { get; set; } = new();

        public List<SleepRecordMemoReportEntryModel> SleepMemos { get; set; } = new();
    }

    /// <summary>
    /// PDF表示用の通所予定・実績。
    /// </summary>
    public class ScheduleReportEntryModel
    {
        public string Session { get; set; } = "";

        public string Location { get; set; } = "";

        public string Status { get; set; } = "";

        public string Activity { get; set; } = "";
    }

    /// <summary>
    /// PDF表示用の体調記録。
    /// </summary>
    public class HealthRecordReportEntryModel
    {
        public string RecordTiming { get; set; } = "";

        public double Condition { get; set; }

        public double Feeling { get; set; }

        public string Symptoms { get; set; } = "";

        public string? Memo { get; set; }
    }

    /// <summary>
    /// PDF表示用の睡眠記録メモ。
    /// </summary>
    public class SleepRecordMemoReportEntryModel
    {
        public string SleepType { get; set; } = "";

        public double Hours { get; set; }

        public string Memo { get; set; } = "";
    }
}
