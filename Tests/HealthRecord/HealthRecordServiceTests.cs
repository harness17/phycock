using Moq;
using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Repository;
using Phycock.Service;
using Xunit;

namespace Tests.HealthRecord
{
    public class HealthRecordServiceTests
    {
        [Fact]
        public void Create_SetsUserId_Correctly()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            HealthRecordEntity? inserted = null;
            repository.Setup(x => x.Insert(It.IsAny<HealthRecordEntity>()))
                .Callback<HealthRecordEntity>(entity => inserted = entity);
            var service = new HealthRecordService(repository.Object);

            service.Create(new HealthRecordFormViewModel
            {
                UserId = "posted-user",
                RecordDate = new DateTime(2026, 5, 3),
                RecordTiming = RecordTiming.Noon,
                SelectedSymptoms = new List<SymptomType> { SymptomType.Headache, SymptomType.Fatigue },
                Condition = ConditionLevel.Bad,
                Feeling = FeelingLevel.Normal,
            }, "current-user");

            Assert.NotNull(inserted);
            Assert.Equal("current-user", inserted.UserId);
            Assert.Equal((long)(SymptomType.Headache | SymptomType.Fatigue), inserted.SymptomFlags);
        }

        [Fact]
        public void Create_AsMember_IgnoresPostedUserId()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            HealthRecordEntity? inserted = null;
            repository.Setup(x => x.Insert(It.IsAny<HealthRecordEntity>()))
                .Callback<HealthRecordEntity>(entity => inserted = entity);
            var service = new HealthRecordService(repository.Object);

            service.Create(new HealthRecordFormViewModel
            {
                UserId = "posted-user",
                RecordDate = new DateTime(2026, 5, 3),
                RecordTiming = RecordTiming.Noon,
                Condition = ConditionLevel.Bad,
                Feeling = FeelingLevel.Normal,
            }, "current-user", isAdmin: false);

            Assert.NotNull(inserted);
            Assert.Equal("current-user", inserted.UserId);
        }

        [Fact]
        public void Create_ReturnsFalse_WhenSameDateAndTimingExists()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            repository.Setup(x => x.ExistsByUserDateTiming("current-user", new DateTime(2026, 5, 3), RecordTiming.Noon, null))
                .Returns(true);
            var service = new HealthRecordService(repository.Object);

            var result = service.Create(new HealthRecordFormViewModel
            {
                UserId = "current-user",
                RecordDate = new DateTime(2026, 5, 3),
                RecordTiming = RecordTiming.Noon,
                Condition = ConditionLevel.Bad,
                Feeling = FeelingLevel.Normal,
            }, "current-user", isAdmin: false);

            Assert.False(result);
            repository.Verify(x => x.Insert(It.IsAny<HealthRecordEntity>()), Times.Never);
        }

        [Fact]
        public void FromFlags_ReturnsCorrectList()
        {
            var flags = (long)(SymptomType.Headache | SymptomType.Fatigue);

            var result = HealthRecordService.FromFlags(flags);

            Assert.Equal(new[] { SymptomType.Headache, SymptomType.Fatigue }, result);
        }

        [Fact]
        public void GetEventsForCalendar_ReturnsCalendarEvents()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            repository.Setup(x => x.GetByUserAndRange("user-1", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)))
                .Returns(new List<HealthRecordEntity>
                {
                    new()
                    {
                        Id = 10,
                        UserId = "user-1",
                        RecordDate = new DateTime(2026, 5, 3),
                        RecordTiming = RecordTiming.Morning,
                        Condition = ConditionLevel.Bad,
                        Feeling = FeelingLevel.Good,
                    },
                });
            var service = new HealthRecordService(repository.Object);

            var result = service.GetEventsForCalendar("user-1", new DateTime(2026, 5, 1), new DateTime(2026, 6, 1));

            var item = Assert.Single(result);
            Assert.Equal("10", item.Id);
            Assert.Equal("2026-05-03", item.Start);
            Assert.Equal("起床時 体調:やや悪い 気分:やや良い", item.Title);
            Assert.True(item.AllDay);
            Assert.Equal("#fd7e14", item.Color);
        }

        [Fact]
        public void GetForEdit_ReturnsNull_WhenNotOwner()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            repository.Setup(x => x.SelectById(10))
                .Returns(new HealthRecordEntity { Id = 10, UserId = "owner-user" });
            var service = new HealthRecordService(repository.Object);

            var result = service.GetForEdit(10, "other-user", isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public void GetForEdit_ReturnsEntity_WhenAdmin()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            repository.Setup(x => x.SelectById(10))
                .Returns(new HealthRecordEntity
                {
                    Id = 10,
                    UserId = "owner-user",
                    RecordDate = new DateTime(2026, 5, 3),
                    Condition = ConditionLevel.Good,
                    Feeling = FeelingLevel.Normal,
                });
            var service = new HealthRecordService(repository.Object);

            var result = service.GetForEdit(10, "admin-user", isAdmin: true);

            Assert.NotNull(result);
            Assert.Equal("owner-user", result.UserId);
        }

        [Fact]
        public void Delete_ReturnsFalse_WhenNotOwner()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            repository.Setup(x => x.SelectById(10))
                .Returns(new HealthRecordEntity { Id = 10, UserId = "owner-user" });
            var service = new HealthRecordService(repository.Object);

            var result = service.Delete(10, "other-user", isAdmin: false);

            Assert.False(result);
            repository.Verify(x => x.LogicalDelete(It.IsAny<HealthRecordEntity>()), Times.Never);
        }

        [Fact]
        public void Update_ReturnsFalse_WhenAnotherRecordHasSameDateAndTiming()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            repository.Setup(x => x.SelectById(10))
                .Returns(new HealthRecordEntity { Id = 10, UserId = "owner-user" });
            repository.Setup(x => x.ExistsByUserDateTiming("owner-user", new DateTime(2026, 5, 3), RecordTiming.Night, 10))
                .Returns(true);
            var service = new HealthRecordService(repository.Object);

            var result = service.Update(new HealthRecordFormViewModel
            {
                Id = 10,
                UserId = "owner-user",
                RecordDate = new DateTime(2026, 5, 3),
                RecordTiming = RecordTiming.Night,
                Condition = ConditionLevel.Normal,
                Feeling = FeelingLevel.Normal,
            }, "owner-user", isAdmin: false);

            Assert.False(result);
            repository.Verify(x => x.Update(It.IsAny<HealthRecordEntity>()), Times.Never);
        }

        [Fact]
        public void GetDisabledRecordTimings_ExcludesCurrentRecord()
        {
            var repository = new Mock<HealthRecordRepository>(null!);
            repository.Setup(x => x.GetByUserAndDate("user-1", new DateTime(2026, 5, 20)))
                .Returns(new List<HealthRecordEntity>
                {
                    new() { Id = 10, UserId = "user-1", RecordTiming = RecordTiming.Morning },
                    new() { Id = 11, UserId = "user-1", RecordTiming = RecordTiming.Noon },
                });
            var service = new HealthRecordService(repository.Object);

            var result = service.GetDisabledRecordTimings("user-1", new DateTime(2026, 5, 20), excludeId: 10);

            Assert.Equal(new[] { RecordTiming.Noon }, result);
        }
    }
}
