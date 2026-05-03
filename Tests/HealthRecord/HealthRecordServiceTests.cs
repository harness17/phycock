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
        public void FromFlags_ReturnsCorrectList()
        {
            var flags = (long)(SymptomType.Headache | SymptomType.Fatigue);

            var result = HealthRecordService.FromFlags(flags);

            Assert.Equal(new[] { SymptomType.Headache, SymptomType.Fatigue }, result);
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
    }
}
