using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Service;
using Phycock.Views.Statistics;
using System.Security.Claims;

namespace Phycock.Controllers
{
    /// <summary>
    /// 期間所感（週次／月次の自己所感）コントローラー。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class PeriodReflectionController : Controller
    {
        private const string DatabaseErrorMessage = "データベース処理中にエラーが発生しました。しばらく時間をおいてから再度お試しください。";

        private readonly PeriodReflectionService _service;
        private readonly UserManagementService _userManagementService;
        private readonly Logger _logger = Logger.GetLogger();

        public PeriodReflectionController(
            PeriodReflectionService service,
            UserManagementService userManagementService)
        {
            _service = service;
            _userManagementService = userManagementService;
        }

        /// <summary>所感を保存する。対象ユーザーはサーバー側で決定（IDOR 対策）。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(PeriodReflectionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    errors = ModelState
                        .Where(kv => kv.Value?.Errors.Count > 0)
                        .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            try
            {
                var targetUserId = await ResolveTargetUserIdAsync();
                if (string.IsNullOrWhiteSpace(targetUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                _service.Save(targetUserId, model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"期間所感の保存に失敗しました。periodType={model.PeriodType}, periodStart={model.PeriodStart:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        /// <summary>
        /// 所感の表示パーシャル HTML を返す。保存後の非同期再描画で使う。
        /// 対象ユーザーはサーバー側で解決（IDOR対策）。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPartial(PeriodType periodType, DateTime periodStart)
        {
            try
            {
                var targetUserId = await ResolveTargetUserIdAsync();
                if (string.IsNullOrWhiteSpace(targetUserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                var vm = _service.GetOrEmpty(targetUserId, periodType, periodStart);
                var title = periodType == PeriodType.Weekly ? "週次の所感" : "月次の所感";
                // 部分Viewは Statistics 配下にあるため絶対パスで指定（Controller名がView検索の起点になるのを回避）
                return PartialView("~/Views/Statistics/_PeriodReflectionDisplay.cshtml", new PeriodReflectionDisplayModel
                {
                    Title = title,
                    Vm = vm
                });
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"期間所感のパーシャル取得に失敗しました。periodType={periodType}, periodStart={periodStart:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, DatabaseErrorMessage);
            }
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private async Task<string> ResolveTargetUserIdAsync()
            => User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();
    }
}
