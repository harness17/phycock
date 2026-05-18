using System.ComponentModel.DataAnnotations;
using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Phycock.Controllers;
using Phycock.Entity;
using Phycock.Models;
using Phycock.Service;
using Xunit;

namespace Tests.UserManagement
{
    public class UserManagementServiceTests
    {
        [Fact]
        public void IsDisabled_ReturnsTrue_WhenUserHasDisableLockoutEnd()
        {
            var user = new ApplicationUser
            {
                LockoutEnabled = true,
                LockoutEnd = UserManagementService.DisabledLockoutEnd,
            };

            var result = UserManagementService.IsDisabled(user);

            Assert.True(result);
        }

        // ポイント: UserManager / RoleManager は多数の依存を取るため、必須の Store のみ実体を渡し残りは null とする
        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
            => new(Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        private static UserManagementService CreateService(Mock<UserManager<ApplicationUser>> userManager)
        {
            var roleManager = new Mock<RoleManager<ApplicationRole>>(
                Mock.Of<IRoleStore<ApplicationRole>>(), null!, null!, null!, null!);
            return new UserManagementService(userManager.Object, roleManager.Object, Mock.Of<IHttpContextAccessor>());
        }

        [Fact]
        public async Task ResetPasswordAsync_Succeeds_WhenUserExists()
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "member1" };
            var userManager = CreateUserManagerMock();
            userManager.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(user);
            userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
            userManager.Setup(x => x.ResetPasswordAsync(user, "reset-token", "NewPass1!"))
                       .ReturnsAsync(IdentityResult.Success);

            var result = await CreateService(userManager).ResetPasswordAsync("user-1", "NewPass1!");

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task ResetPasswordAsync_Fails_WhenUserNotFound()
        {
            var userManager = CreateUserManagerMock();
            userManager.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

            var result = await CreateService(userManager).ResetPasswordAsync("missing", "NewPass1!");

            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task ResetPasswordAsync_PropagatesIdentityError_WhenPolicyViolation()
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "member1" };
            var userManager = CreateUserManagerMock();
            userManager.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(user);
            userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
            userManager.Setup(x => x.ResetPasswordAsync(user, "reset-token", "weak"))
                       .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "パスワードが短すぎます。" }));

            var result = await CreateService(userManager).ResetPasswordAsync("user-1", "weak");

            Assert.False(result.Succeeded);
        }

        [Fact]
        public void ResetPasswordViewModel_RequiresNewPassword()
        {
            var model = new UserManagementResetPasswordViewModel { Id = "user-1", UserName = "member1" };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, x => x.MemberNames.Contains(nameof(UserManagementResetPasswordViewModel.NewPassword)));
        }

        [Fact]
        public void ResetPasswordViewModel_RequiresMatchingConfirmPassword()
        {
            var model = new UserManagementResetPasswordViewModel
            {
                Id = "user-1",
                UserName = "member1",
                NewPassword = "NewPass1!",
                ConfirmPassword = "Different1!",
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, x => x.MemberNames.Contains(nameof(UserManagementResetPasswordViewModel.ConfirmPassword)));
        }

        [Fact]
        public void UserManagementController_IsAdminOnly()
        {
            var authorize = Assert.Single(
                typeof(UserManagementController).GetCustomAttributes(false).OfType<AuthorizeAttribute>());

            Assert.Equal("Admin", authorize.Roles);
        }
    }
}
