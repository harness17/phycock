using Moq;
using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Repository;
using Phycock.Service;
using Xunit;

namespace Tests.Dashboard
{
    public class DashboardServiceTests
    {
        private static DashboardService BuildService(
            Mock<HealthRecordRepository> healthRepo,
            Mock<SleepRecordRepository> sleepRepo,
            Mock<ScheduleEntryRepository> scheduleRepo)
        {
            var healthService = new HealthRecordService(healthRepo.Object);
            var sleepService = new SleepRecordService(sleepRepo.Object);
            var scheduleService = new ScheduleEntryService(scheduleRepo.Object);
            return new DashboardService(healthService, sleepService, scheduleService);
        }

        [Fact]
        public void GetDashboard_WithHealthRecords_SetsLatestConditionAndFeeling()
        {
            var healthRepo = new Mock<HealthRecordRepository>(null!);
            var sleepRepo = new Mock<SleepRecordRepository>(null!);
            var scheduleRepo = new Mock<ScheduleEntryRepository>(null!);

            // GetTodaySummary calls GetByUserAndDate with DateTime.Today
            healthRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>
                {
                    new()
                    {
                        Id = 1,
                        UserId = "user-1",
                        RecordDate = DateTime.Today,
                        RecordTiming = RecordTiming.Morning,
                        Condition = ConditionLevel.Bad,
                        Feeling = FeelingLevel.Normal,
                    },
                    new()
                    {
                        Id = 2,
                        UserId = "user-1",
                        RecordDate = DateTime.Today,
                        RecordTiming = RecordTiming.Noon,
                        Condition = ConditionLevel.Good,
                        Feeling = FeelingLevel.VeryGood,
                    },
                });
            // GetWeeklySummary calls GetByUserAndRange
            healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>());

            var service = BuildService(healthRepo, sleepRepo, scheduleRepo);

            var result = service.GetDashboard("user-1", isAdmin: false);

            // LatestCondition and LatestFeeling should come from the last health record
            Assert.Equal(ConditionLevel.Good, result.LatestCondition);
            Assert.Equal(FeelingLevel.VeryGood, result.LatestFeeling);
        }

        [Fact]
        public void GetDashboard_WithNoHealthRecords_LatestConditionIsNull()
        {
            var healthRepo = new Mock<HealthRecordRepository>(null!);
            var sleepRepo = new Mock<SleepRecordRepository>(null!);
            var scheduleRepo = new Mock<ScheduleEntryRepository>(null!);

            healthRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>());

            var service = BuildService(healthRepo, sleepRepo, scheduleRepo);

            var result = service.GetDashboard("user-1", isAdmin: false);

            Assert.Null(result.LatestCondition);
            Assert.Null(result.LatestFeeling);
        }

        [Fact]
        public void GetDashboard_WithSleepRecord_HasSleepRecordIsTrue()
        {
            var healthRepo = new Mock<HealthRecordRepository>(null!);
            var sleepRepo = new Mock<SleepRecordRepository>(null!);
            var scheduleRepo = new Mock<ScheduleEntryRepository>(null!);

            healthRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>
                {
                    new()
                    {
                        Id = 10,
                        UserId = "user-1",
                        RecordDate = DateTime.Today,
                        StartDate = DateTime.Today.AddHours(22),
                        SleepType = SleepType.NightSleep,
                    },
                });
            sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>());

            var service = BuildService(healthRepo, sleepRepo, scheduleRepo);

            var result = service.GetDashboard("user-1", isAdmin: false);

            Assert.True(result.HasSleepRecord);
        }

        [Fact]
        public void GetDashboard_WithNoSleepRecord_HasSleepRecordIsFalse()
        {
            var healthRepo = new Mock<HealthRecordRepository>(null!);
            var sleepRepo = new Mock<SleepRecordRepository>(null!);
            var scheduleRepo = new Mock<ScheduleEntryRepository>(null!);

            healthRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>());

            var service = BuildService(healthRepo, sleepRepo, scheduleRepo);

            var result = service.GetDashboard("user-1", isAdmin: false);

            Assert.False(result.HasSleepRecord);
        }

        [Fact]
        public void GetDashboard_WithNoSchedule_TodayScheduleEntriesIsEmpty_AndIsUnavailableIsFalse()
        {
            var healthRepo = new Mock<HealthRecordRepository>(null!);
            var sleepRepo = new Mock<SleepRecordRepository>(null!);
            var scheduleRepo = new Mock<ScheduleEntryRepository>(null!);

            healthRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", It.IsAny<DateOnly>()))
                .Returns(new List<ScheduleEntryEntity>());

            var service = BuildService(healthRepo, sleepRepo, scheduleRepo);

            var result = service.GetDashboard("user-1", isAdmin: false);

            Assert.Empty(result.TodayScheduleEntries);
            Assert.False(result.IsUnavailable);
        }
    }
}
