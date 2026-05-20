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
            Assert.Equal(new[] { "任意時刻 07:15", "訓練開始時" }, day.HealthRecords.Select(x => x.TimingLabel));
        }

        [Fact]
        public void GetMonthlyCalendar_IncludesCustomTimingInHealthSummary()
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
                        RecordTiming = RecordTiming.Custom,
                        RecordTime = new TimeOnly(14, 35),
                        Condition = ConditionLevel.Good,
                        Feeling = FeelingLevel.Normal,
                    },
                });
            sleepRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>());
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object, scheduleRepository.Object);

            var result = service.GetMonthlyCalendar("user-1", 2026, 5);

            var cell = Assert.Single(result.Cells, x => x.Date == new DateTime(2026, 5, 3));
            Assert.Equal("任意時刻 14:35", cell.HealthTimingSummary);
        }
    }
}
