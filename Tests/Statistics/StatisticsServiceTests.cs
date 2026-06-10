using Moq;
using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Repository;
using Phycock.Service;
using Xunit;

namespace Tests.Statistics
{
    public class StatisticsServiceTests
    {
        [Fact]
        public void GetWeeklyHealthStats_Returns7DayData()
        {
            var healthRepository = new Mock<HealthRecordRepository>(null!);
            var sleepRepository = new Mock<SleepRecordRepository>(null!);
            healthRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>
                {
                    new()
                    {
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 3),
                        Condition = ConditionLevel.Good,
                        Feeling = FeelingLevel.Normal,
                    },
                });
            var scheduleRepository = new Mock<ScheduleEntryRepository>(null!);
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object, scheduleRepository.Object);

            var result = service.GetWeeklyHealthStats("user-1", new DateTime(2026, 5, 1));

            Assert.Equal(7, result.Labels.Count);
            Assert.Equal(7, result.ConditionData.Count);
            Assert.Equal(4, result.ConditionData[2]);
        }

        [Fact]
        public void GetWeeklyHealthStats_ExcludesOtherUsersData()
        {
            var healthRepository = new Mock<HealthRecordRepository>(null!);
            var sleepRepository = new Mock<SleepRecordRepository>(null!);
            healthRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            var scheduleRepository = new Mock<ScheduleEntryRepository>(null!);
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object, scheduleRepository.Object);

            var result = service.GetWeeklyHealthStats("user-1", new DateTime(2026, 5, 1));

            Assert.All(result.ConditionData, Assert.Null);
            healthRepository.Verify(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public void GetWeeklyReport_OrdersCustomTimingByRecordTimeAndDisplaysTime()
        {
            var healthRepository = new Mock<HealthRecordRepository>(null!);
            var sleepRepository = new Mock<SleepRecordRepository>(null!);
            var scheduleRepository = new Mock<ScheduleEntryRepository>(null!);
            healthRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>
                {
                    new()
                    {
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 3),
                        RecordTiming = RecordTiming.Noon,
                        Condition = ConditionLevel.Good,
                        Feeling = FeelingLevel.Normal,
                    },
                    new()
                    {
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 3),
                        RecordTiming = RecordTiming.Custom,
                        RecordTime = new TimeOnly(7, 15),
                        Condition = ConditionLevel.Bad,
                        Feeling = FeelingLevel.Normal,
                    },
                });
            sleepRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>());
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object, scheduleRepository.Object);

            var result = service.GetWeeklyReport("user-1", new DateTime(2026, 5, 3));

            var day = Assert.Single(result.Days, x => x.Date == new DateTime(2026, 5, 3));
            Assert.Equal(new[] { "07:15", "訓練開始時" }, day.HealthRecords.Select(x => x.TimingLabel));
        }

        [Fact]
        public void GetWeeklyReport_AllDayScheduleUsesSingleTimelineBar()
        {
            var healthRepository = new Mock<HealthRecordRepository>(null!);
            var sleepRepository = new Mock<SleepRecordRepository>(null!);
            var scheduleRepository = new Mock<ScheduleEntryRepository>(null!);
            healthRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>
                {
                    new()
                    {
                        UserId = "user-1",
                        Date = new DateOnly(2026, 5, 3),
                        Session = ScheduleSession.AllDay,
                        Status = ScheduleStatus.Planned,
                        ActivityType = ActivityType.Program,
                    },
                });
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object, scheduleRepository.Object);

            var result = service.GetWeeklyReport("user-1", new DateTime(2026, 5, 3));

            Assert.Equal(new double[] { 9.5, 15.5 }, Assert.Single(result.TimelineChart.ScheduleAm[0]));
            Assert.Empty(result.TimelineChart.SchedulePm[0]);
        }

        [Fact]
        public void GetWeeklyReport_MultipleEntriesInSameCategoryUseSeparateTimelineBars()
        {
            var healthRepository = new Mock<HealthRecordRepository>(null!);
            var sleepRepository = new Mock<SleepRecordRepository>(null!);
            var scheduleRepository = new Mock<ScheduleEntryRepository>(null!);
            healthRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>
                {
                    new()
                    {
                        UserId = "user-1",
                        Date = new DateOnly(2026, 6, 9),
                        Session = ScheduleSession.AM,
                        Status = ScheduleStatus.Attended,
                        ActivityType = ActivityType.Program,
                        StartTime = new TimeOnly(9, 30),
                        EndTime = new TimeOnly(10, 30),
                    },
                    new()
                    {
                        UserId = "user-1",
                        Date = new DateOnly(2026, 6, 9),
                        Session = ScheduleSession.AM,
                        Status = ScheduleStatus.Attended,
                        ActivityType = ActivityType.IndividualTraining,
                        StartTime = new TimeOnly(11, 0),
                        EndTime = new TimeOnly(12, 0),
                    },
                });
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object, scheduleRepository.Object);

            var result = service.GetWeeklyReport("user-1", new DateTime(2026, 6, 7));

            var june9Index = result.Days.FindIndex(x => x.Date == new DateTime(2026, 6, 9));
            Assert.Equal(
                new List<double[]>
                {
                    new[] { 9.5, 10.5 },
                    new[] { 11.0, 12.0 },
                },
                result.TimelineChart.ScheduleAm[june9Index]);
        }

        [Fact]
        public void GetWeeklyReport_AmAndPmAbsencesUseSeparateTimelineBars()
        {
            var healthRepository = new Mock<HealthRecordRepository>(null!);
            var sleepRepository = new Mock<SleepRecordRepository>(null!);
            var scheduleRepository = new Mock<ScheduleEntryRepository>(null!);
            healthRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>
                {
                    new()
                    {
                        UserId = "user-1",
                        Date = new DateOnly(2026, 6, 9),
                        Session = ScheduleSession.AM,
                        Status = ScheduleStatus.Absent,
                        ActivityType = ActivityType.Program,
                    },
                    new()
                    {
                        UserId = "user-1",
                        Date = new DateOnly(2026, 6, 9),
                        Session = ScheduleSession.PM,
                        Status = ScheduleStatus.Absent,
                        ActivityType = ActivityType.Program,
                    },
                });
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object, scheduleRepository.Object);

            var result = service.GetWeeklyReport("user-1", new DateTime(2026, 6, 7));

            var june9Index = result.Days.FindIndex(x => x.Date == new DateTime(2026, 6, 9));
            Assert.Equal(
                new List<double[]>
                {
                    new[] { 9.5, 12.5 },
                    new[] { 13.5, 15.5 },
                },
                result.TimelineChart.ScheduleAbsent[june9Index]);
        }
    }
}
