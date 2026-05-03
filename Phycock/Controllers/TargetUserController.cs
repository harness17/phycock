using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Service;

namespace Phycock.Controllers
{
    /// <summary>
    /// Admin が操作対象の Member を切り替えるためのコントローラー。
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class TargetUserController : Controller
    {
        private readonly UserManagementService _userManagementService;

        public TargetUserController(UserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        /// <summary>
        /// 選択中の操作対象 Member をセッションに保存する。
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Select(string targetUserId, string? returnUrl)
        {
            await _userManagementService.SetSelectedMemberUserIdAsync(targetUserId);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
