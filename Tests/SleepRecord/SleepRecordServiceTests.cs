using Moq;
using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Repository;
using Phycock.Service;
using Xunit;

namespace Tests.SleepRecord
{
    public class SleepRecordServiceTests
    {
        [Fact]
        public void GetSleepDuration_CalculatesCorrectly()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>
                {
                    new()
                    {
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 1),
                        StartDate = new DateTime(2026, 5, 1, 22, 0, 0),
                        EndDate = new DateTime(2026, 5, 2, 6, 0, 0),
                        SleepType = SleepType.NightSleep,
                    },
                    new()
                    {
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 2),
                        StartDate = new DateTime(2026, 5, 2, 13, 0, 0),
                        EndDate = new DateTime(2026, 5, 2, 14, 30, 0),
                        SleepType = SleepType.DaytimeNap,
                    },
                    new()
                    {
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 3),
                        StartDate = new DateTime(2026, 5, 3, 22, 0, 0),
                        EndDate = null,
                        SleepType = SleepType.NightSleep,
                    },
                });
            var service = new SleepRecordService(repository.Object);

            var result = service.GetSleepDuration("user-1", new DateTime(2026, 5, 1), new DateTime(2026, 5, 7));

            Assert.Equal(TimeSpan.FromHours(9.5), result);
        }

        [Fact]
        public void GetForEditAsync_ReturnsNull_WhenNotOwner()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            repository.Setup(x => x.SelectById(10))
                .Returns(new SleepRecordEntity { Id = 10, UserId = "owner-user" });
            var service = new SleepRecordService(repository.Object);

            var result = service.GetForEditAsync(10, "other-user", isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public void CreateAsync_AsMember_IgnoresPostedUserId()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            SleepRecordEntity? inserted = null;
            repository.Setup(x => x.Insert(It.IsAny<SleepRecordEntity>()))
                .Callback<SleepRecordEntity>(entity => inserted = entity);
            var service = new SleepRecordService(repository.Object);

            service.CreateAsync(new SleepRecordFormViewModel
            {
                UserId = "posted-user",
                RecordDate = new DateTime(2026, 5, 3),
                StartDate = new DateTime(2026, 5, 3, 22, 0, 0),
                SleepType = SleepType.NightSleep,
            }, "current-user", isAdmin: false);

            Assert.NotNull(inserted);
            Assert.Equal("current-user", inserted.UserId);
        }

        [Fact]
        public void CreateAsync_CombinesRecordDateAndTimes_WithOvernightEnd()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            SleepRecordEntity? inserted = null;
            repository.Setup(x => x.Insert(It.IsAny<SleepRecordEntity>()))
                .Callback<SleepRecordEntity>(entity => inserted = entity);
            var service = new SleepRecordService(repository.Object);

            service.CreateAsync(new SleepRecordFormViewModel
            {
                UserId = "current-user",
                RecordDate = new DateTime(2026, 5, 3),
                StartTime = new TimeOnly(22, 30),
                EndTime = new TimeOnly(6, 15),
                SleepType = SleepType.NightSleep,
            }, "current-user");

            Assert.NotNull(inserted);
            Assert.Equal(new DateTime(2026, 5, 3, 22, 30, 0), inserted.StartDate);
            Assert.Equal(new DateTime(2026, 5, 4, 6, 15, 0), inserted.EndDate);
        }
    }
}
