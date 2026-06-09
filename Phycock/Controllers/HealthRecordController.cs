using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Models;
using Phycock.Service;
using System.Security.Claims;

namespace Phycock.Controllers
{
    /// <summary>
    /// 体調記録コントローラー。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class HealthRecordController : Controller
    {
        private readonly HealthRecordService _service;
        private readonly UserManagementService _userManagementService;

        public HealthRecordController(HealthRecordService service, UserManagementService userManagementService)
        {
            _service = service;
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? filterDate)
        {
            var date = filterDate ?? DateTime.Today;
            var isAdmin = User.IsInRole("Admin");
            ViewBag.FilterDate = date.ToString("yyyy-MM-dd");
            ViewBag.IsAdmin = isAdmin;

            var records = isAdmin
                ? _service.GetList(await _userManagementService.GetSelectedMemberUserIdAsync(), date)
                : _service.GetList(GetCurrentUserId(), date);
            return View(records);
        }

        [HttpGet]
        public IActionResult Detail(long id)
        {
            var detail = _service.GetDetail(id, GetCurrentUserId(), User.IsInRole("Admin"));
            if (detail == null) return StatusCode(StatusCodes.Status403Forbidden);
            return Json(detail);
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            var userId = User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();

            return Json(_service.GetEventsForCalendar(userId, start, end));
        }

        /// <summary>
        /// ヒートマップ用の日別体調レベルデータを返す。
        /// start〜end の範囲は最大42日（FullCalendar 月表示 = 最大6週）。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> HeatmapData(DateTime start, DateTime end)
        {
            if ((end - start).TotalDays > 42)
            {
                return BadRequest(new { error = "取得範囲は最大42日です。" });
            }

            var userId = User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();

            return Json(_service.GetHeatmapData(userId, start, end));
        }

        [HttpGet]
        public async Task<IActionResult> Create(DateTime? recordDate)
        {
            var userId = User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId)) return StatusCode(StatusCodes.Status403Forbidden);
            return View(_service.BuildCreateForm(userId, recordDate));
        }

        [HttpGet]
        public async Task<IActionResult> GetDisabledTimings(DateTime recordDate, long? id)
        {
            var userId = User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();

            if (id.HasValue)
            {
                var existing = _service.GetForEdit(id.Value, GetCurrentUserId(), User.IsInRole("Admin"));
                if (existing == null) return StatusCode(StatusCodes.Status403Forbidden);
                userId = existing.UserId;
            }

            var disabled = _service.GetDisabledRecordTimings(userId, recordDate, id)
                .Select(x => ((int)x).ToString())
                .ToList();

            return Json(disabled);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HealthRecordFormViewModel model)
        {
            if (User.IsInRole("Admin"))
            {
                model.UserId = await _userManagementService.GetSelectedMemberUserIdAsync();
                if (string.IsNullOrWhiteSpace(model.UserId)) return StatusCode(StatusCodes.Status403Forbidden);
            }
            else
            {
                model.UserId = GetCurrentUserId();
            }

            ValidateDuplicate(model);

            if (!ModelState.IsValid)
            {
                _service.FillSelections(model);
                return View(model);
            }

            if (!_service.Create(model, GetCurrentUserId(), User.IsInRole("Admin")))
            {
                AddDuplicateModelError();
                _service.FillSelections(model);
                return View(model);
            }

            return RedirectToAction(nameof(Index), new { filterDate = model.RecordDate.ToString("yyyy-MM-dd") });
        }

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var model = _service.GetForEdit(id, GetCurrentUserId(), User.IsInRole("Admin"));
            if (model == null) return StatusCode(StatusCodes.Status403Forbidden);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(HealthRecordFormViewModel model)
        {
            var existing = _service.GetForEdit(model.Id, GetCurrentUserId(), User.IsInRole("Admin"));
            if (existing == null) return StatusCode(StatusCodes.Status403Forbidden);
            model.UserId = existing.UserId;

            ValidateDuplicate(model);

            if (!ModelState.IsValid)
            {
                _service.FillSelections(model);
                return View(model);
            }

            var updated = _service.Update(model, GetCurrentUserId(), User.IsInRole("Admin"));
            if (!updated) return StatusCode(StatusCodes.Status403Forbidden);
            return RedirectToAction(nameof(Index), new { filterDate = model.RecordDate.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            var deleted = _service.Delete(id, GetCurrentUserId(), User.IsInRole("Admin"));
            if (!deleted) return StatusCode(StatusCodes.Status403Forbidden);
            return RedirectToAction(nameof(Index));
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private void ValidateDuplicate(HealthRecordFormViewModel model)
        {
            if (model.RecordTiming == Phycock.Entity.Enums.RecordTiming.Custom && !model.RecordTime.HasValue)
            {
                var now = DateTime.Now;
                model.RecordTime = new TimeOnly(now.Hour, now.Minute);
            }

            if (_service.IsDuplicate(model.UserId, model.RecordDate, model.RecordTiming, model.RecordTime, model.Id == 0 ? null : model.Id))
                AddDuplicateModelError();
        }

        private void AddDuplicateModelError()
            => ModelState.AddModelError(nameof(HealthRecordFormViewModel.RecordTiming), "同じ日の同じタイミングまたは任意時刻の体調記録は既に登録されています。");
    }
}
