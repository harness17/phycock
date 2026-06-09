using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Phycock.Controllers;
using Phycock.Entity;
using Phycock.Repository;
using Phycock.Service;
using System.Security.Claims;
using Xunit;

namespace Tests.Statistics
{
    public class StatisticsControllerTests
    {
        private const string MemberUserId = "member-1";

        private static StatisticsController CreateController(string role = "Member")
        {
            var healthRepo = new Mock<HealthRecordRepository>(null!);
            var sleepRepo = new Mock<SleepRecordRepository>(null!);
            var scheduleRepo = new Mock<ScheduleEntryRepository>(null!);

            healthRepo.Setup(x => x.GetByUserAndRange(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            sleepRepo.Setup(x => x.GetByUserAndRange(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());

            var service = new StatisticsService(healthRepo.Object, sleepRepo.Object, scheduleRepo.Object);

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(), null!, null!, null!, null!);
            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            // Role=Member なので ResolveTargetUserIdAsync は GetCurrentUserId() を呼ぶだけ
            // GetSelectedMemberUserIdAsync は non-virtual のためモックできないが不要
            var userManagementService = new UserManagementService(
                userManagerMock.Object, roleManagerMock.Object, httpContextAccessorMock.Object);

            var pdfExportService = new Mock<PdfExportService>(null!, null!);
            var periodReflectionService = new Mock<PeriodReflectionService>(null!);

            var controller = new StatisticsController(
                service,
                userManagementService,
                pdfExportService.Object,
                periodReflectionService.Object);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, MemberUserId),
                new(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        [Fact]
        public async Task TrendData_ValidWeekStart_ReturnsJsonResult()
        {
            var controller = CreateController();
            var weekStart = DateTime.Today;

            var result = await controller.TrendData(weekStart);

            Assert.IsType<JsonResult>(result);
        }

        [Fact]
        public async Task TrendData_WeekStartTooFarInPast_ReturnsBadRequest()
        {
            var controller = CreateController();
            // NormalizeWeekStart は最大6日前の日曜日へ寄せるが過去方向は安全なため -366 で十分
            var weekStart = DateTime.Today.AddDays(-366);

            var result = await controller.TrendData(weekStart);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task TrendData_WeekStartTooFarInFuture_ReturnsBadRequest()
        {
            var controller = CreateController();
            // NormalizeWeekStart は最大6日前の日曜日へ寄せるため、余裕を持って 372 日後を使う
            var weekStart = DateTime.Today.AddDays(372);

            var result = await controller.TrendData(weekStart);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }
    }
}
