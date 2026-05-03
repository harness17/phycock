using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Phycock.Controllers;
using Phycock.Models;
using Xunit;

namespace Tests.RootError
{
    public class RootErrorControllerTests
    {
        [Fact]
        public void Error_ReturnsGenericMessage_AndDoesNotExposeExceptionDetails()
        {
            var controller = CreateController();

            var result = Assert.IsType<ViewResult>(controller.Error());
            var model = Assert.IsType<ErrorViewModel>(result.Model);

            Assert.Equal(StatusCodes.Status500InternalServerError, controller.Response.StatusCode);
            Assert.Equal(500, model.StatusCode);
            Assert.Equal("サーバーエラーが発生しました", model.ErrorTitle);
            Assert.DoesNotContain("Exception", model.ErrorMessage);
            Assert.DoesNotContain("SQL", model.ErrorMessage);
        }

        [Fact]
        public void StatusCode_ForForbidden_SetsResponseStatusAndGenericAccessDeniedMessage()
        {
            var controller = CreateController();

            var result = Assert.IsType<ViewResult>(controller.StatusCode(403));
            var model = Assert.IsType<ErrorViewModel>(result.Model);

            Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
            Assert.Equal(403, model.StatusCode);
            Assert.Equal("アクセスが拒否されました", model.ErrorTitle);
            Assert.DoesNotContain("Path", model.ErrorMessage);
        }

        private static RootErrorController CreateController()
        {
            return new RootErrorController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }
    }
}
