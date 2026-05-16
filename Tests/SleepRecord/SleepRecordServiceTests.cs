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
        public void GetEventsForCalendar_ReturnsCalendarEvents()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)))
                .Returns(new List<SleepRecordEntity>
                {
                    new()
                    {
                        Id = 20,
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 3),
                        StartDate = new DateTime(2026, 5, 3, 22, 0, 0),
                        EndDate = new DateTime(2026, 5, 4, 6, 0, 0),
                        SleepType = SleepType.NightSleep,
                    },
                });
            var service = new SleepRecordService(repository.Object);

            var result = service.GetEventsForCalendar("user-1", new DateTime(2026, 5, 1), new DateTime(2026, 6, 1));

            var item = Assert.Single(result);
            Assert.Equal("20", item.Id);
            Assert.Equal("本睡眠", item.Title);
            Assert.Equal("2026-05-03T22:00:00", item.Start);
            Assert.Equal("2026-05-04T06:00:00", item.End);
            Assert.Equal("#E9D8FD", item.Color);
            Assert.Equal("#6F42C1", item.BorderColor);
            Assert.Equal("#31135E", item.TextColor);
            Assert.Equal("本睡眠", item.ExtendedProps.PrimaryText);
            Assert.Equal("22:00-06:00 8.0h", item.ExtendedProps.SecondaryText);
            Assert.Equal(0, item.ExtendedProps.SortOrder);
        }

        [Fact]
        public void GetEventsForCalendar_DaytimeNap_SetsSortOrderFromStartTime()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)))
                .Returns(new List<SleepRecordEntity>
                {
                    new()
                    {
                        Id = 21,
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 3),
                        StartDate = new DateTime(2026, 5, 3, 13, 15, 0),
                        EndDate = new DateTime(2026, 5, 3, 13, 45, 0),
                        SleepType = SleepType.DaytimeNap,
                    },
                });
            var service = new SleepRecordService(repository.Object);

            var result = service.GetEventsForCalendar("user-1", new DateTime(2026, 5, 1), new DateTime(2026, 6, 1));

            var item = Assert.Single(result);
            Assert.Equal("仮眠", item.Title);
            Assert.Equal(795, item.ExtendedProps.SortOrder);
        }

        [Fact]
        public void GetForEdit_ReturnsNull_WhenNotOwner()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            repository.Setup(x => x.SelectById(10))
                .Returns(new SleepRecordEntity { Id = 10, UserId = "owner-user" });
            var service = new SleepRecordService(repository.Object);

            var result = service.GetForEdit(10, "other-user", isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public void Create_AsMember_IgnoresPostedUserId()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            SleepRecordEntity? inserted = null;
            repository.Setup(x => x.Insert(It.IsAny<SleepRecordEntity>()))
                .Callback<SleepRecordEntity>(entity => inserted = entity);
            var service = new SleepRecordService(repository.Object);

            service.Create(new SleepRecordFormViewModel
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
        public void Create_CombinesRecordDateAndTimes_WithOvernightEnd()
        {
            var repository = new Mock<SleepRecordRepository>(null!);
            SleepRecordEntity? inserted = null;
            repository.Setup(x => x.Insert(It.IsAny<SleepRecordEntity>()))
                .Callback<SleepRecordEntity>(entity => inserted = entity);
            var service = new SleepRecordService(repository.Object);

            service.Create(new SleepRecordFormViewModel
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
