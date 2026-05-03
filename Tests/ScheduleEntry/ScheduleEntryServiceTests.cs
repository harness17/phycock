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
            Assert.Equal(new TimeOnly(9, 0), result.StartTime);
            Assert.Equal(new TimeOnly(12, 0), result.EndTime);
        }
    }
}
