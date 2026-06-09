using Phycock.Models;

namespace Phycock.Service
{
    /// <summary>
    /// ダッシュボードサービス。
    /// </summary>
    public class DashboardService
    {
        private readonly HealthRecordService _healthRecordService;
        private readonly SleepRecordService _sleepRecordService;
        private readonly ScheduleEntryService _scheduleEntryService;

        /// <summary>
        /// ダッシュボードサービスを初期化する。
        /// </summary>
        public DashboardService(
            HealthRecordService healthRecordService,
            SleepRecordService sleepRecordService,
            ScheduleEntryService scheduleEntryService)
        {
            _healthRecordService = healthRecordService;
            _sleepRecordService = sleepRecordService;
            _scheduleEntryService = scheduleEntryService;
        }

        /// <summary>ダッシュボード表示データを取得する。</summary>
        public DashboardViewModel GetDashboard(string userId, bool isAdmin)
        {
            var weeklySummary = _healthRecordService.GetWeeklySummary(userId);
            weeklySummary.TotalSleepDuration = _sleepRecordService.GetSleepDuration(
                userId,
                weeklySummary.StartDate,
                weeklySummary.EndDate);

            var todayHealthRecords = _healthRecordService.GetTodaySummary(userId);
            var todaySleepRecords = _sleepRecordService.GetList(userId, DateTime.Today);
            var latestRecord = todayHealthRecords.Count > 0 ? todayHealthRecords[^1] : null;

            return new DashboardViewModel
            {
                TodayScheduleEntries = _scheduleEntryService.GetTodayEntries(userId),
                TodayHealthRecords = todayHealthRecords,
                WeeklySummary = weeklySummary,
                HasSleepRecord = todaySleepRecords.Count > 0,
                LatestCondition = latestRecord?.Condition,
                LatestFeeling = latestRecord?.Feeling,
            };
        }
    }
}
