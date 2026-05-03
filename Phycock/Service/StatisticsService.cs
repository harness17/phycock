using Phycock.Models;
using Phycock.Repository;

namespace Phycock.Service
{
    /// <summary>
    /// 統計サービス。
    /// </summary>
    public class StatisticsService
    {
        private readonly HealthRecordRepository _healthRecordRepository;
        private readonly SleepRecordRepository _sleepRecordRepository;

        /// <summary>
        /// 統計サービスを初期化する。
        /// </summary>
        public StatisticsService(
            HealthRecordRepository healthRecordRepository,
            SleepRecordRepository sleepRecordRepository)
        {
            _healthRecordRepository = healthRecordRepository;
            _sleepRecordRepository = sleepRecordRepository;
        }

        /// <summary>週次体調統計を取得する。</summary>
        public ChartSeriesDto GetWeeklyHealthStats(string userId, DateTime weekStart)
        {
            var start = weekStart.Date;
            var end = start.AddDays(6);
            var records = _healthRecordRepository.GetByUserAndRange(userId, start, end);
            var result = new ChartSeriesDto();

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                var dayRecords = records.Where(x => x.RecordDate.Date == day).ToList();
                result.Labels.Add(day.ToString("MM/dd"));
                result.ConditionData.Add(dayRecords.Count == 0 ? null : Math.Round(dayRecords.Average(x => (int)x.Condition), 2));
                result.FeelingData.Add(dayRecords.Count == 0 ? null : Math.Round(dayRecords.Average(x => (int)x.Feeling), 2));
            }

            return result;
        }

        /// <summary>月次体調統計を取得する。</summary>
        public ChartSeriesDto GetMonthlyHealthStats(string userId, int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var records = _healthRecordRepository.GetByUserAndRange(userId, start, end);
            var result = new ChartSeriesDto();

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                var dayRecords = records.Where(x => x.RecordDate.Date == day).ToList();
                result.Labels.Add(day.ToString("MM/dd"));
                result.ConditionData.Add(dayRecords.Count == 0 ? null : Math.Round(dayRecords.Average(x => (int)x.Condition), 2));
                result.FeelingData.Add(dayRecords.Count == 0 ? null : Math.Round(dayRecords.Average(x => (int)x.Feeling), 2));
            }

            return result;
        }

        /// <summary>週次睡眠統計を取得する。</summary>
        public ChartSeriesDto GetWeeklySleepStats(string userId, DateTime weekStart)
        {
            var start = weekStart.Date;
            var end = start.AddDays(6);
            var records = _sleepRecordRepository.GetByUserAndRange(userId, start, end);
            var result = new ChartSeriesDto();

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                var total = records
                    .Where(x => x.RecordDate.Date == day)
                    .Where(x => x.EndDate.HasValue && x.EndDate.Value > x.StartDate)
                    .Aggregate(TimeSpan.Zero, (sum, record) => sum + (record.EndDate!.Value - record.StartDate));

                result.Labels.Add(day.ToString("MM/dd"));
                result.SleepHoursData.Add(Math.Round(total.TotalHours, 2));
            }

            return result;
        }
    }
}
