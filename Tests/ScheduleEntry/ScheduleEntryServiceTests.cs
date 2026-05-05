using Moq;
using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Repository;
using Phycock.Service;
using Xunit;

namespace Tests.ScheduleEntry
{
    public class ScheduleEntryServiceTests
    {
        [Fact]
        public void Create_AsMember_IgnoresPostedUserId()
        {
            var repository = new Mock<ScheduleEntryRepository>(null!);
            ScheduleEntryEntity? inserted = null;
            repository.Setup(x => x.Insert(It.IsAny<ScheduleEntryEntity>()))
                .Callback<ScheduleEntryEntity>(entity => inserted = entity);
            var service = new ScheduleEntryService(repository.Object);

            service.Create(new ScheduleEntryFormViewModel
            {
                UserId = "posted-user",
                Date = new DateOnly(2026, 5, 3),
                Session = ScheduleSession.AM,
                Status = ScheduleStatus.Planned,
                ActivityType = ActivityType.Program,
            }, "current-user", isAdmin: false);

            Assert.NotNull(inserted);
            Assert.Equal("current-user", inserted.UserId);
        }

        [Fact]
        public void BuildCreateForm_SetsAmTimePreset()
        {
            var repository = new Mock<ScheduleEntryRepository>(null!);
            var service = new ScheduleEntryService(repository.Object);

            var result = service.BuildCreateForm("user-1", new DateOnly(2026, 5, 3));

            Assert.Equal(ScheduleSession.AM, result.Session);
            Assert.Equal(ActivityType.Program, result.ActivityType);
            Assert.Equal(ProgramType.SelfWork, result.ProgramType);
            Assert.Equal(new TimeOnly(9, 0), result.StartTime);
            Assert.Equal(new TimeOnly(12, 0), result.EndTime);
        }

        [Fact]
        public void GetEventsForCalendar_ProgramEntry_SetsBackgroundBorderAndTextColors()
        {
            var repository = new Mock<ScheduleEntryRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)))
                .Returns(new List<ScheduleEntryEntity>
                {
                    new()
                    {
                        Id = 1,
                        UserId = "user-1",
                        Date = new DateOnly(2026, 5, 3),
                        Session = ScheduleSession.AM,
                        ActivityType = ActivityType.Program,
                        ProgramType = ProgramType.ApplicationInterview,
                        ActivityNote = "応募書類",
                    },
                });
            var service = new ScheduleEntryService(repository.Object);

            var result = service.GetEventsForCalendar("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            var item = Assert.Single(result);
            Assert.Equal("#F4CCCC", item.BackgroundColor);
            Assert.Equal("#C00000", item.BorderColor);
            Assert.Equal("#7F1D1D", item.TextColor);
            Assert.Equal(item.BackgroundColor, item.Color);
        }

        [Fact]
        public void GetEventsForCalendar_NonProgramEntry_SetsActivityTypeColors()
        {
            var repository = new Mock<ScheduleEntryRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)))
                .Returns(new List<ScheduleEntryEntity>
                {
                    new()
                    {
                        Id = 2,
                        UserId = "user-1",
                        Date = new DateOnly(2026, 5, 4),
                        Session = ScheduleSession.PM,
                        ActivityType = ActivityType.IndividualTraining,
                    },
                });
            var service = new ScheduleEntryService(repository.Object);

            var result = service.GetEventsForCalendar("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            var item = Assert.Single(result);
            Assert.Equal("#F4E5F8", item.BackgroundColor);
            Assert.Equal("#8E44AD", item.BorderColor);
            Assert.Equal("#4A235A", item.TextColor);
        }

        [Fact]
        public void GetEventsForCalendar_AtHomeEntry_UsesAtHomeColors()
        {
            var repository = new Mock<ScheduleEntryRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)))
                .Returns(new List<ScheduleEntryEntity>
                {
                    new()
                    {
                        Id = 3,
                        UserId = "user-1",
                        Date = new DateOnly(2026, 5, 5),
                        Session = ScheduleSession.AllDay,
                        IsAtHome = true,
                        ActivityType = ActivityType.Program,
                        ProgramType = ProgramType.ApplicationInterview,
                    },
                });
            var service = new ScheduleEntryService(repository.Object);

            var result = service.GetEventsForCalendar("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            var item = Assert.Single(result);
            Assert.True(item.ExtendedProps.IsAtHome);
            Assert.Equal("#DDEFEF", item.BackgroundColor);
            Assert.Equal("#2C9A9A", item.BorderColor);
            Assert.Equal("#134F4F", item.TextColor);
        }

        [Fact]
        public void GetEventsForCalendar_OtherProgramEntry_UsesVisibleOtherColors()
        {
            var repository = new Mock<ScheduleEntryRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)))
                .Returns(new List<ScheduleEntryEntity>
                {
                    new()
                    {
                        Id = 4,
                        UserId = "user-1",
                        Date = new DateOnly(2026, 5, 6),
                        Session = ScheduleSession.PM,
                        ActivityType = ActivityType.Program,
                        ProgramType = ProgramType.OtherFreeInput,
                    },
                });
            var service = new ScheduleEntryService(repository.Object);

            var result = service.GetEventsForCalendar("user-1", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

            var item = Assert.Single(result);
            Assert.Equal("#FFFFFF", item.BackgroundColor);
            Assert.Equal("#ADB5BD", item.BorderColor);
            Assert.Equal("#343A40", item.TextColor);
        }
    }
}
