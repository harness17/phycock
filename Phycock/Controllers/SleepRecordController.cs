using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Models;
using Phycock.Service;
using System.Security.Claims;

namespace Phycock.Controllers
{
    /// <summary>
    /// 睡眠記録コントローラー。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class SleepRecordController : Controller
    {
        private readonly SleepRecordService _service;
        private readonly UserManagementService _userManagementService;

        public SleepRecordController(SleepRecordService service, UserManagementService userManagementService)
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
                ? _service.GetListAsync(await _userManagementService.GetSelectedMemberUserIdAsync(), date)
                : _service.GetListAsync(GetCurrentUserId(), date);
            return View(records);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SleepRecordFormViewModel model)
        {
            ValidateSleepRange(model);
            if (!ModelState.IsValid) return View(model);

            if (User.IsInRole("Admin"))
            {
                model.UserId = await _userManagementService.GetSelectedMemberUserIdAsync();
                if (string.IsNullOrWhiteSpace(model.UserId)) return StatusCode(StatusCodes.Status403Forbidden);
            }

            _service.CreateAsync(model, GetCurrentUserId(), User.IsInRole("Admin"));
            return RedirectToAction(nameof(Index), new { filterDate = model.RecordDate.ToString("yyyy-MM-dd") });
        }

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var model = _service.GetForEditAsync(id, GetCurrentUserId(), User.IsInRole("Admin"));
            if (model == null) return StatusCode(StatusCodes.Status403Forbidden);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(SleepRecordFormViewModel model)
        {
            ValidateSleepRange(model);
            if (!ModelState.IsValid) return View(model);

            var updated = _service.UpdateAsync(model, GetCurrentUserId(), User.IsInRole("Admin"));
            if (!updated) return StatusCode(StatusCodes.Status403Forbidden);
            return RedirectToAction(nameof(Index), new { filterDate = model.RecordDate.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            var deleted = _service.DeleteAsync(id, GetCurrentUserId(), User.IsInRole("Admin"));
            if (!deleted) return StatusCode(StatusCodes.Status403Forbidden);
            return RedirectToAction(nameof(Index));
        }

        private void ValidateSleepRange(SleepRecordFormViewModel model)
        {
            var (startDate, endDate) = SleepRecordService.BuildSleepDateTimes(model.RecordDate, model.StartTime, model.EndTime);
            model.StartDate = startDate;
            model.EndDate = endDate;
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    }
}
