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
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object);

            var result = service.GetWeeklyHealthStatsAsync("user-1", new DateTime(2026, 5, 1));

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
            var service = new StatisticsService(healthRepository.Object, sleepRepository.Object);

            var result = service.GetWeeklyHealthStatsAsync("user-1", new DateTime(2026, 5, 1));

            Assert.All(result.ConditionData, Assert.Null);
            healthRepository.Verify(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }
    }
}
