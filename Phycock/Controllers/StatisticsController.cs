using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Service;
using System.Security.Claims;

namespace Phycock.Controllers
{
    /// <summary>
    /// 統計コントローラー。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class StatisticsController : Controller
    {
        private const string DatabaseErrorMessage = "データベース処理中にエラーが発生しました。しばらく時間をおいてから再度お試しください。";

        private readonly StatisticsService _service;
        private readonly Logger _logger = Logger.GetLogger();

        public StatisticsController(StatisticsService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetHealthWeekly(DateTime weekStart)
        {
            try
            {
                return Json(_service.GetWeeklyHealthStats(GetCurrentUserId(), weekStart));
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"週次体調統計の取得中にエラーが発生しました。weekStart={weekStart:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        [HttpGet]
        public IActionResult GetHealthMonthly(int year, int month)
        {
            try
            {
                return Json(_service.GetMonthlyHealthStats(GetCurrentUserId(), year, month));
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"月次体調統計の取得中にエラーが発生しました。year={year}, month={month}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        [HttpGet]
        public IActionResult GetSleepWeekly(DateTime weekStart)
        {
            try
            {
                return Json(_service.GetWeeklySleepStats(GetCurrentUserId(), weekStart));
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"週次睡眠統計の取得中にエラーが発生しました。weekStart={weekStart:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    }
}
