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

            return new DashboardViewModel
            {
                TodayScheduleEntries = _scheduleEntryService.GetTodayEntries(userId),
                TodayHealthRecords = _healthRecordService.GetTodaySummary(userId),
                WeeklySummary = weeklySummary,
            };
        }
    }
}
