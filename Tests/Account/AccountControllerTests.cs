using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Phycock.Controllers;
using Phycock.Models;
using Phycock.Common;
using Xunit;

namespace Tests.Account
{
    public class AccountControllerTests
    {
        [Fact]
        public void Register_Get_IsAdminOnly()
        {
            var method = typeof(AccountController).GetMethod(nameof(AccountController.Register), Type.EmptyTypes);

            Assert.NotNull(method);
            Assert.DoesNotContain(method.GetCustomAttributes(false), x => x is AllowAnonymousAttribute);
            var authorize = Assert.Single(method.GetCustomAttributes(false).OfType<AuthorizeAttribute>());
            Assert.Equal("Admin", authorize.Roles);
        }

        [Fact]
        public void Register_Post_IsAdminOnly()
        {
            var method = typeof(AccountController).GetMethod(nameof(AccountController.Register), new[] { typeof(RegisterViewModel) });

            Assert.NotNull(method);
            Assert.DoesNotContain(method.GetCustomAttributes(false), x => x is AllowAnonymousAttribute);
            var authorize = Assert.Single(method.GetCustomAttributes(false).OfType<AuthorizeAttribute>());
            Assert.Equal("Admin", authorize.Roles);
        }

        [Fact]
        public void RegisterViewModel_RequiresUserName()
        {
            var model = new RegisterViewModel
            {
                Email = "member@example.com",
                Password = "Member1!",
                ConfirmPassword = "Member1!",
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, x => x.MemberNames.Contains(nameof(RegisterViewModel.UserName)));
        }

        [Fact]
        public void UserManagementEditViewModel_RequiresSingleRole()
        {
            var model = new UserManagementEditViewModel
            {
                Id = "user-1",
                UserName = "member",
                Email = "member@example.com",
            };

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, x => x.MemberNames.Contains(nameof(UserManagementEditViewModel.RoleName)));
        }

        [Fact]
        public void IdentityOptions_RequireUniqueEmail_IsEnabled()
        {
            Assert.True(IdentityOptionsSnapshot.RequireUniqueEmail);
        }
    }
}
