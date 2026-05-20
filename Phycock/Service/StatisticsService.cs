using Phycock.Common;
using Phycock.Entity;
using Phycock.Entity.Enums;
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
        private readonly ScheduleEntryRepository _scheduleEntryRepository;

        /// <summary>
        /// 統計サービスを初期化する。
        /// </summary>
        public StatisticsService(
            HealthRecordRepository healthRecordRepository,
            SleepRecordRepository sleepRecordRepository,
            ScheduleEntryRepository scheduleEntryRepository)
        {
            _healthRecordRepository = healthRecordRepository;
            _sleepRecordRepository = sleepRecordRepository;
            _scheduleEntryRepository = scheduleEntryRepository;
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

        // ───────────────────────────────────────────────────────────
        // 週次レポート（DB データから生成、画面・PDF 共用）
        // ───────────────────────────────────────────────────────────

        private static readonly string[] DayOfWeekShort = { "日", "月", "火", "水", "木", "金", "土" };

        /// <summary>
        /// 週次レポートを取得する。1週間（日曜開始）のデータを集計し、
        /// テーブル表示・スケジュールストリップ・チャート描画用データを一括で構築する。
        /// </summary>
        public WeeklyReportDto GetWeeklyReport(string userId, DateTime weekStart)
        {
            var start = weekStart.Date;
            var end = start.AddDays(6);
            var endDateOnly = DateOnly.FromDateTime(end);
            var startDateOnly = DateOnly.FromDateTime(start);

            // 日跨ぎ睡眠を捕捉するため前日からも引く
            var sleepStart = start.AddDays(-1);
            var healthRecords = _healthRecordRepository.GetByUserAndRange(userId, start, end);
            var sleepRecords = _sleepRecordRepository.GetByUserAndRange(userId, sleepStart, end);
            var scheduleEntries = _scheduleEntryRepository.GetByUserAndRange(userId, startDateOnly, endDateOnly);

            var report = new WeeklyReportDto { WeekStart = start };
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                var daily = BuildDailyReport(day, healthRecords, sleepRecords, scheduleEntries);
                report.Days.Add(daily);
            }

            BuildReportChart(report);
            BuildTimelineChart(report, sleepRecords, scheduleEntries);
            return report;
        }

        /// <summary>月次カレンダー用の日別集計を取得する。</summary>
        public MonthlyCalendarDto GetMonthlyCalendar(string userId, int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
            var gridEnd = monthEnd.AddDays(6 - (int)monthEnd.DayOfWeek);

            var sleepStart = monthStart.AddDays(-1);
            var healthRecords = _healthRecordRepository.GetByUserAndRange(userId, monthStart, monthEnd);
            var sleepRecords = _sleepRecordRepository.GetByUserAndRange(userId, sleepStart, monthEnd);
            var scheduleEntries = _scheduleEntryRepository.GetByUserAndRange(
                userId,
                DateOnly.FromDateTime(monthStart),
                DateOnly.FromDateTime(monthEnd));

            var calendar = new MonthlyCalendarDto
            {
                Year = year,
                Month = month
            };

            // チャート用：当月内の日別集計（週次レポートチャートと同仕様）
            var inMonthDays = new List<DailyReportDto>();

            for (var day = gridStart; day <= gridEnd; day = day.AddDays(1))
            {
                var inMonth = day >= monthStart && day <= monthEnd;
                var daily = inMonth
                    ? BuildDailyReport(day, healthRecords, sleepRecords, scheduleEntries)
                    : new DailyReportDto { Date = day };
                var sleepTotal = Math.Round(daily.NightSleepHours + daily.OtherSleepHours, 2);

                if (inMonth) inMonthDays.Add(daily);

                calendar.Cells.Add(new MonthlyDayCellDto
                {
                    Date = day,
                    InMonth = inMonth,
                    ConditionAvg = inMonth ? daily.ConditionAvg : null,
                    FeelingAvg = inMonth ? daily.FeelingAvg : null,
                    SleepTotalHours = inMonth ? sleepTotal : 0,
                    SleepLevel = inMonth ? SleepStandards.Classify(sleepTotal) : SleepLevel.None,
                    ScheduleDayClass = inMonth ? daily.ScheduleDayClass : "rest",
                    ScheduleSummary = inMonth && daily.ScheduleSummaryLines.Count > 0
                        ? string.Join("、", daily.ScheduleSummaryLines)
                        : "予定なし"
                });
            }

            FillReportChart(calendar.ReportChart, inMonthDays);
            calendar.SleepConditionByBand = BuildSleepConditionByBand(inMonthDays);
            return calendar;
        }

        /// <summary>帯別棒グラフの集計用ワーク。1睡眠帯あたり1インスタンス。</summary>
        private sealed class SleepBandAccumulator
        {
            public List<double> Conditions { get; } = new();
            public List<double> NightSleepHours { get; } = new();
            public List<double> OtherSleepHours { get; } = new();
            public int AttendDayCount { get; set; }
            public int RemoteDayCount { get; set; }
            public int PrivateDayCount { get; set; }
            public int RestDayCount { get; set; }
        }

        /// <summary>睡眠4帯の定義（不足→過剰の固定順）。</summary>
        private static readonly (SleepLevel Level, string Label)[] SleepBandDefs =
        {
            (SleepLevel.Insufficient,  "不足 (〜6h)"),
            (SleepLevel.SlightlyShort, "やや不足 (6-7h)"),
            (SleepLevel.Adequate,      "適正 (7-9h)"),
            (SleepLevel.Excessive,     "過剰 (9h〜)")
        };

        /// <summary>
        /// 前夜の睡眠時間帯ごとの「翌日の体調平均」を構築する。
        /// 前夜の睡眠を4帯に分け、各帯の翌日の体調平均を集計する。
        /// 月初日は前夜データが無いため除外。体調記録の無い日・前夜睡眠記録の無い日も除外。
        /// </summary>
        private static SleepConditionByBandDto BuildSleepConditionByBand(List<DailyReportDto> inMonthDays)
        {
            var accByLevel = new Dictionary<SleepLevel, SleepBandAccumulator>
            {
                [SleepLevel.Insufficient] = new(),
                [SleepLevel.SlightlyShort] = new(),
                [SleepLevel.Adequate] = new(),
                [SleepLevel.Excessive] = new()
            };

            for (var i = 1; i < inMonthDays.Count; i++)
            {
                var prev = inMonthDays[i - 1];
                var current = inMonthDays[i];
                var prevSleep = Math.Round(prev.NightSleepHours + prev.OtherSleepHours, 2);

                // 前夜の睡眠記録が無い／当日の体調記録が無いペアは除外
                if (prevSleep <= 0 || current.ConditionAvg is not double condition) continue;

                var level = SleepStandards.Classify(prevSleep);
                if (!accByLevel.TryGetValue(level, out var acc)) continue;

                acc.Conditions.Add(condition);
                acc.NightSleepHours.Add(prev.NightSleepHours);
                acc.OtherSleepHours.Add(prev.OtherSleepHours);
                switch (current.ScheduleDayClass)
                {
                    case "planned": acc.AttendDayCount++; break;
                    case "remote": acc.RemoteDayCount++; break;
                    case "private": acc.PrivateDayCount++; break;
                    default: acc.RestDayCount++; break;
                }
            }

            var dto = new SleepConditionByBandDto();
            foreach (var (level, label) in SleepBandDefs)
            {
                var acc = accByLevel[level];
                var conds = acc.Conditions;
                dto.Bands.Add(new SleepBandStatDto
                {
                    Level = level,
                    Label = label,
                    DayCount = conds.Count,
                    AverageCondition = conds.Count > 0 ? Math.Round(conds.Average(), 2) : null,
                    AverageNightSleepHours = conds.Count > 0 ? Math.Round(acc.NightSleepHours.Average(), 2) : null,
                    AverageOtherSleepHours = conds.Count > 0 ? Math.Round(acc.OtherSleepHours.Average(), 2) : null,
                    AttendDayCount = acc.AttendDayCount,
                    RemoteDayCount = acc.RemoteDayCount,
                    PrivateDayCount = acc.PrivateDayCount,
                    RestDayCount = acc.RestDayCount
                });
            }
            dto.TotalDayCount = dto.Bands.Sum(b => b.DayCount);

            // 要約：体調平均が集計できた帯が2つ以上あれば、最高帯と最低帯を1文で示す
            var measured = dto.Bands.Where(b => b.AverageCondition is not null).ToList();
            if (measured.Count >= 2)
            {
                var best = measured.MaxBy(b => b.AverageCondition)!;
                var worst = measured.MinBy(b => b.AverageCondition)!;
                dto.LeadText = best.Level == worst.Level
                    ? "集計できた帯では翌日の体調平均に大きな差は見られません。"
                    : $"前夜が「{best.Label}」の翌日は体調平均 {best.AverageCondition:0.0} で最も高く、"
                      + $"「{worst.Label}」の翌日は {worst.AverageCondition:0.0} で最も低い傾向です。";
            }
            return dto;
        }

        private static DailyReportDto BuildDailyReport(
            DateTime day,
            List<HealthRecordEntity> allHealth,
            List<SleepRecordEntity> allSleep,
            List<ScheduleEntryEntity> allSchedule)
        {
            var dayDate = DateOnly.FromDateTime(day);
            var dailyHealth = allHealth.Where(x => x.RecordDate.Date == day)
                                       .OrderBy(HealthRecordService.GetTimingSortOrder)
                                       .ThenBy(x => x.CreateDate)
                                       .ToList();
            var dailySchedule = allSchedule.Where(x => x.Date == dayDate)
                                           .OrderBy(x => x.Session)
                                           .ToList();

            // 睡眠記録：当日 RecordDate のもの + 前日に始まり当日に終わるもの
            var dailySleep = allSleep
                .Where(x => x.RecordDate.Date == day
                            || (x.EndDate.HasValue && x.StartDate.Date < day && x.EndDate.Value.Date >= day))
                .OrderBy(x => x.SleepType)
                .ThenBy(x => x.StartDate)
                .ToList();

            // 体調・気分平均
            double? conditionAvg = dailyHealth.Count == 0 ? null
                : Math.Round(dailyHealth.Average(x => (int)x.Condition), 2);
            double? feelingAvg = dailyHealth.Count == 0 ? null
                : Math.Round(dailyHealth.Average(x => (int)x.Feeling), 2);

            // 睡眠時間（本睡眠 / 他睡眠）。当日に重なる時間のみ加算。
            var dayStart = day;
            var dayEnd = day.AddDays(1);
            double nightHours = 0, otherHours = 0;
            foreach (var s in dailySleep.Where(s => s.EndDate.HasValue))
            {
                var overlap = OverlapHours(s.StartDate, s.EndDate!.Value, dayStart, dayEnd);
                if (overlap <= 0) continue;
                if (s.SleepType == SleepType.NightSleep) nightHours += overlap;
                else otherHours += overlap;
            }

            var dto = new DailyReportDto
            {
                Date = day,
                DayLabel = $"{day.Month}/{day.Day} {DayOfWeekShort[(int)day.DayOfWeek]}",
                ConditionAvg = conditionAvg,
                FeelingAvg = feelingAvg,
                NightSleepHours = Math.Round(nightHours, 2),
                OtherSleepHours = Math.Round(otherHours, 2)
            };

            // スケジュールサマリ・ストリップ・日種別
            BuildScheduleParts(dailySchedule, dto);

            // 体調記録明細
            foreach (var h in dailyHealth)
            {
                dto.HealthRecords.Add(new HealthRecordItemDto
                {
                    TimingLabel = HealthRecordService.FormatTiming(h),
                    ConditionLabel = ((int)h.Condition).ToString(),
                    FeelingLabel = ((int)h.Feeling).ToString(),
                    SymptomsLabel = FormatSymptoms(h.SymptomFlags),
                    Memo = string.IsNullOrWhiteSpace(h.Memo) ? null : h.Memo
                });
            }

            // 睡眠記録明細：本睡眠 → 他睡眠 の順に表示
            foreach (var s in dailySleep.Where(s => s.EndDate.HasValue))
            {
                var hours = Math.Round(OverlapHours(s.StartDate, s.EndDate!.Value, dayStart, dayEnd), 2);
                if (hours <= 0) continue;
                dto.SleepRecords.Add(new SleepRecordItemDto
                {
                    TypeLabel = s.SleepType == SleepType.NightSleep ? "本睡眠" : "他睡眠",
                    Hours = hours,
                    Memo = string.IsNullOrWhiteSpace(s.Memo) ? null : s.Memo
                });
            }

            return dto;
        }

        /// <summary>SymptomFlags（ビットフラグ）を「眠気、頭痛」等の文字列に整形する。</summary>
        private static string FormatSymptoms(long flags)
        {
            if (flags <= 0) return "なし";
            var names = Enum.GetValues<SymptomType>()
                .Where(s => s != SymptomType.None && ((long)s & flags) != 0)
                .Select(s => s.GetDisplayName())
                .ToList();
            return names.Count == 0 ? "なし" : string.Join("、", names);
        }

        /// <summary>スケジュールサマリ（テーブル列）・ストリップ・日種別CSSを構築する。</summary>
        private static void BuildScheduleParts(List<ScheduleEntryEntity> daySchedule, DailyReportDto dto)
        {
            if (daySchedule.Count == 0)
            {
                dto.ScheduleSummaryLines.Add("予定なし");
                dto.ScheduleDayClass = "rest";
                dto.ScheduleStrip.Add(new ScheduleStripItemDto
                {
                    SessionLabel = "予定なし",
                    DetailLabel = "",
                    StatusClass = "status-none"
                });
                return;
            }

            // 日種別: プライベートのみの日は private、通所/在宅の予定があれば
            // プライベートを除いて判定（通所/在宅優先）。通所が1件でも含まれれば planned、全部 AtHome なら remote。
            var attendanceEntries = daySchedule.Where(x => x.ActivityType != ActivityType.Private).ToList();
            dto.ScheduleDayClass = attendanceEntries.Count == 0
                ? "private"
                : attendanceEntries.All(x => x.IsAtHome) ? "remote" : "planned";

            var summaryParts = new List<string>();
            foreach (var entry in daySchedule)
            {
                var sessionPrefix = entry.Session switch
                {
                    ScheduleSession.AM => "AM",
                    ScheduleSession.PM => "PM",
                    _ => "終日"
                };
                var venue = entry.IsAtHome ? "在宅" : "通所";
                var statusName = entry.Status.GetDisplayName();
                var activityName = entry.ActivityType == ActivityType.Program && entry.ProgramType.HasValue
                    ? entry.ProgramType.Value.GetDisplayName()
                    : entry.ActivityType.GetDisplayName();

                // テーブル「通所」列のサマリ。プライベートは通所実績ではないので種別名で表示する。
                if (entry.ActivityType == ActivityType.Private)
                {
                    summaryParts.Add($"{sessionPrefix} プライベート");
                }
                else if (entry.Status == ScheduleStatus.Planned)
                {
                    summaryParts.Add($"{sessionPrefix} {venue}予定");
                }
                else
                {
                    summaryParts.Add($"{sessionPrefix} {statusName}");
                }

                // ストリップ
                dto.ScheduleStrip.Add(new ScheduleStripItemDto
                {
                    SessionLabel = $"{sessionPrefix} {venue}",
                    DetailLabel = $"{statusName} / {activityName}",
                    StatusClass = MapStatusClass(entry.Status)
                });
            }

            dto.ScheduleSummaryLines = summaryParts;
        }

        private static string MapStatusClass(ScheduleStatus status) => status switch
        {
            ScheduleStatus.Attended => "status-attended",
            ScheduleStatus.Planned => "status-planned",
            ScheduleStatus.Late => "status-late",
            ScheduleStatus.EarlyLeave => "status-early-leave",
            ScheduleStatus.Absent => "status-absent",
            _ => "status-none"
        };

        /// <summary>2区間の重なり時間（時間単位）を返す。</summary>
        private static double OverlapHours(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        {
            var s = aStart > bStart ? aStart : bStart;
            var e = aEnd < bEnd ? aEnd : bEnd;
            return e > s ? (e - s).TotalHours : 0;
        }

        /// <summary>上部チャート用の系列データを Days から構築する。</summary>
        private static void BuildReportChart(WeeklyReportDto report)
            => FillReportChart(report.ReportChart, report.Days);

        /// <summary>日別集計から複合チャート用データ（体調・気分・睡眠内訳）を構築する。週次・月次共用。</summary>
        private static void FillReportChart(WeeklyReportChartDto chart, IEnumerable<DailyReportDto> days)
        {
            foreach (var d in days)
            {
                chart.Labels.Add($"{d.Date.Month}/{d.Date.Day}");
                chart.Condition.Add(d.ConditionAvg);
                chart.Feeling.Add(d.FeelingAvg);
                chart.NightSleep.Add(d.NightSleepHours);
                chart.OtherSleep.Add(d.OtherSleepHours);
            }
        }

        /// <summary>タイムラインチャート用データを構築する（時間帯ベース、日跨ぎ睡眠は前後分割）。</summary>
        private static void BuildTimelineChart(
            WeeklyReportDto report,
            List<SleepRecordEntity> allSleep,
            List<ScheduleEntryEntity> allSchedule)
        {
            foreach (var d in report.Days)
            {
                report.TimelineChart.Labels.Add($"{d.Date.Month}/{d.Date.Day} ({DayOfWeekShort[(int)d.Date.DayOfWeek]})");
            }

            // 各日について、当日 0-24h 軸での区間を構築
            for (int i = 0; i < report.Days.Count; i++)
            {
                var day = report.Days[i].Date;
                var dayStart = day;
                var dayEnd = day.AddDays(1);

                // 当日の朝（前日からの本睡眠が当日0時を跨いで終わる場合）
                double? earlyStart = null, earlyEnd = null;
                // 当日の夜（当日中に始まり当日中or翌日朝まで続く本睡眠）
                double? lateStart = null, lateEnd = null;

                foreach (var s in allSleep.Where(x => x.SleepType == SleepType.NightSleep && x.EndDate.HasValue))
                {
                    // 前夜開始 → 当日朝終了
                    if (s.StartDate < dayStart && s.EndDate!.Value > dayStart && s.EndDate.Value <= dayEnd)
                    {
                        earlyStart = 0;
                        earlyEnd = (s.EndDate.Value - dayStart).TotalHours;
                    }
                    // 当日深夜開始（22-24時台）→ 翌朝終了
                    else if (s.StartDate >= dayStart && s.StartDate < dayEnd && s.EndDate!.Value > dayEnd)
                    {
                        lateStart = (s.StartDate - dayStart).TotalHours;
                        lateEnd = 24;
                    }
                    // 当日中で完結する本睡眠（短い昼の本睡眠など）
                    else if (s.StartDate >= dayStart && s.EndDate!.Value <= dayEnd)
                    {
                        // 早朝寄り or 深夜寄りで割り当て
                        var startH = (s.StartDate - dayStart).TotalHours;
                        var endH = (s.EndDate.Value - dayStart).TotalHours;
                        if (startH < 12) { earlyStart ??= startH; earlyEnd ??= endH; }
                        else { lateStart ??= startH; lateEnd ??= endH; }
                    }
                }
                report.TimelineChart.NightSleepEarly.Add(earlyEnd.HasValue ? new double?[] { earlyStart, earlyEnd } : null);
                report.TimelineChart.NightSleepLate.Add(lateEnd.HasValue ? new double?[] { lateStart, lateEnd } : null);

                // 他睡眠（仮眠等）：当日内で複数あれば最初の1件を表示
                var otherFirst = allSleep
                    .Where(x => x.SleepType != SleepType.NightSleep && x.EndDate.HasValue
                                && x.StartDate >= dayStart && x.EndDate!.Value <= dayEnd)
                    .OrderBy(x => x.StartDate)
                    .FirstOrDefault();
                report.TimelineChart.OtherSleep.Add(otherFirst is not null
                    ? new double?[]
                    {
                        (otherFirst.StartDate - dayStart).TotalHours,
                        (otherFirst.EndDate!.Value - dayStart).TotalHours
                    }
                    : null);

                // スケジュール: 当日分のうち AM/PM、欠席分は別系列で破線表示
                var todaySchedule = allSchedule.Where(x => x.Date == DateOnly.FromDateTime(day)).ToList();
                var amEntries = todaySchedule
                    .Where(x => x.Session == ScheduleSession.AM && x.Status != ScheduleStatus.Absent).ToList();
                var pmEntries = todaySchedule
                    .Where(x => x.Session == ScheduleSession.PM && x.Status != ScheduleStatus.Absent).ToList();
                var am = amEntries.FirstOrDefault(x => x.ActivityType != ActivityType.Private);
                var pm = pmEntries.FirstOrDefault(x => x.ActivityType != ActivityType.Private);
                var amPrivate = amEntries.FirstOrDefault(x => x.ActivityType == ActivityType.Private);
                var pmPrivate = pmEntries.FirstOrDefault(x => x.ActivityType == ActivityType.Private);
                var absent = todaySchedule.FirstOrDefault(x => x.Status == ScheduleStatus.Absent);

                report.TimelineChart.ScheduleAm.Add(MakeScheduleBar(am, 9.5, 12.5));
                report.TimelineChart.SchedulePm.Add(MakeScheduleBar(pm, 13.5, 15.5));
                report.TimelineChart.ScheduleAmPrivate.Add(MakeScheduleBar(amPrivate, 9.5, 12.5));
                report.TimelineChart.SchedulePmPrivate.Add(MakeScheduleBar(pmPrivate, 13.5, 15.5));
                report.TimelineChart.ScheduleAbsent.Add(absent is null ? null
                    : MakeScheduleBar(absent,
                        absent.Session == ScheduleSession.AM ? 9.5 : 13.5,
                        absent.Session == ScheduleSession.AM ? 12.5 : 15.5));
            }
        }

        /// <summary>スケジュールエントリを [start_h, end_h] のタイムラインバーに変換する。</summary>
        private static double?[]? MakeScheduleBar(ScheduleEntryEntity? entry, double defaultStart, double defaultEnd)
        {
            if (entry is null) return null;
            var s = entry.StartTime?.ToTimeSpan().TotalHours ?? defaultStart;
            var e = entry.EndTime?.ToTimeSpan().TotalHours ?? defaultEnd;
            if (e <= s) return null;
            return new double?[] { s, e };
        }
    }
}
