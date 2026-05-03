using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Models;
using Phycock.Service;
using System.Diagnostics;
using System.Security.Claims;

namespace Phycock.Controllers
{
    /// <summary>
    /// ホームコントローラー。ログイン後のトップ画面とプライバシーポリシーを提供する。
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly UserManagementService _userManagementService;

        public HomeController(DashboardService dashboardService, UserManagementService userManagementService)
        {
            _dashboardService = dashboardService;
            _userManagementService = userManagementService;
        }

        /// <summary>トップ画面。</summary>
        public async Task<IActionResult> Index()
        {
            var userId = User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();
            var vm = string.IsNullOrWhiteSpace(userId)
                ? new DashboardViewModel()
                : _dashboardService.GetDashboard(userId, User.IsInRole("Admin"));
            return View(vm);
        }

        /// <summary>プライバシーポリシー画面。未ログインでも閲覧可能。</summary>
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// フォールバック用エラー画面。
        /// 主エラーハンドラーは RootErrorController.Error / StatusCode だが、
        /// 旧スキャフォールドとの互換用に残している。
        /// </summary>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    }
}
