using Dev.CommonLibrary.Entity;
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
    }
}
