using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Models;
using Phycock.Service;
using System.Security.Claims;

namespace Phycock.Controllers
{
    /// <summary>
    /// 通所予定コントローラー。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class ScheduleEntryController : Controller
    {
        private const string DatabaseErrorMessage = "データベース処理中にエラーが発生しました。しばらく時間をおいてから再度お試しください。";

        private readonly ScheduleEntryService _service;
        private readonly UserManagementService _userManagementService;
        private readonly Logger _logger = Logger.GetLogger();

        public ScheduleEntryController(ScheduleEntryService service, UserManagementService userManagementService)
        {
            _service = service;
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            try
            {
                var userId = await ResolveTargetUserIdAsync();
                var events = _service.GetEventsForCalendar(
                    userId,
                    DateOnly.FromDateTime(start),
                    DateOnly.FromDateTime(end.AddDays(-1)));
                return Json(events);
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"通所予定の取得中にエラーが発生しました。start={start:O}, end={end:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        [HttpGet]
        public IActionResult Detail(long id)
        {
            try
            {
                var detail = _service.GetDetail(id, GetCurrentUserId(), User.IsInRole("Admin"));
                if (detail == null) return StatusCode(StatusCodes.Status403Forbidden);
                return Json(detail);
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"通所予定詳細の取得中にエラーが発生しました。id={id}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreatePartial(DateTime? date)
        {
            var dateOnly = date.HasValue ? DateOnly.FromDateTime(date.Value) : (DateOnly?)null;
            var userId = await ResolveTargetUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId)) return StatusCode(StatusCodes.Status403Forbidden);
            return PartialView("_CreatePartial", _service.BuildCreateForm(userId, dateOnly));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduleEntryFormViewModel model)
        {
            ValidateProgramType(model);
            ValidateTimeRange(model);
            if (!ModelState.IsValid) return PartialView("_CreatePartial", model);

            try
            {
                if (User.IsInRole("Admin"))
                {
                    model.UserId = await _userManagementService.GetSelectedMemberUserIdAsync();
                    if (string.IsNullOrWhiteSpace(model.UserId)) return StatusCode(StatusCodes.Status403Forbidden);
                }

                _service.Create(model, GetCurrentUserId(), User.IsInRole("Admin"));
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel("通所予定の作成中にエラーが発生しました。"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, error = DatabaseErrorMessage });
            }
        }

        [HttpGet]
        public IActionResult EditPartial(long id)
        {
            var model = _service.GetForEdit(id, GetCurrentUserId(), User.IsInRole("Admin"));
            if (model == null) return StatusCode(StatusCodes.Status403Forbidden);
            return PartialView("_EditPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ScheduleEntryFormViewModel model)
        {
            ValidateProgramType(model);
            ValidateTimeRange(model);
            if (!ModelState.IsValid) return PartialView("_EditPartial", model);

            try
            {
                var updated = _service.Update(model, GetCurrentUserId(), User.IsInRole("Admin"));
                if (!updated) return StatusCode(StatusCodes.Status403Forbidden);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"通所予定の更新中にエラーが発生しました。id={model.Id}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, error = DatabaseErrorMessage });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            try
            {
                var deleted = _service.Delete(id, GetCurrentUserId(), User.IsInRole("Admin"));
                if (!deleted) return StatusCode(StatusCodes.Status403Forbidden);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"通所予定の削除中にエラーが発生しました。id={id}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, error = DatabaseErrorMessage });
            }
        }

        private void ValidateTimeRange(ScheduleEntryFormViewModel model)
        {
            if (model.StartTime.HasValue && model.EndTime.HasValue && model.EndTime.Value <= model.StartTime.Value)
                ModelState.AddModelError(nameof(model.EndTime), "終了時刻は開始時刻より後に設定してください。");
        }

        private void ValidateProgramType(ScheduleEntryFormViewModel model)
        {
            if (model.ActivityType == Phycock.Entity.Enums.ActivityType.Program && !model.ProgramType.HasValue)
                ModelState.AddModelError(nameof(model.ProgramType), "プログラム種別を選択してください。");
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private async Task<string> ResolveTargetUserIdAsync()
            => User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();
    }
}
