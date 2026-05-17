using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Phycock.Models;
using Phycock.Service;
using PWCookie = Microsoft.Playwright.Cookie;
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
        private readonly UserManagementService _userManagementService;
        private readonly PdfExportService _pdfExportService;
        private readonly Logger _logger = Logger.GetLogger();

        public StatisticsController(
            StatisticsService service,
            UserManagementService userManagementService,
            PdfExportService pdfExportService)
        {
            _service = service;
            _userManagementService = userManagementService;
            _pdfExportService = pdfExportService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? weekStart)
        {
            try
            {
                var ws = NormalizeWeekStart(weekStart);
                var userId = await ResolveTargetUserIdAsync();
                var vm = new StatisticsViewModel
                {
                    WeeklyReport = _service.GetWeeklyReport(userId, ws),
                    MonthlyCalendar = _service.GetMonthlyCalendar(userId, ws.Year, ws.Month)
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"統計ページ表示中にエラー。weekStart={weekStart:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, DatabaseErrorMessage);
            }
        }

        /// <summary>weekStart を直近の日曜日 00:00 に正規化する。null/不正値は今週の日曜。</summary>
        private static DateTime NormalizeWeekStart(DateTime? weekStart)
        {
            var d = (weekStart ?? DateTime.Today).Date;
            return d.AddDays(-(int)d.DayOfWeek);
        }

        [HttpGet]
        public async Task<IActionResult> GetHealthWeekly(DateTime weekStart)
        {
            try
            {
                return Json(_service.GetWeeklyHealthStats(await ResolveTargetUserIdAsync(), weekStart));
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"週次体調統計の取得中にエラーが発生しました。weekStart={weekStart:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSleepWeekly(DateTime weekStart)
        {
            try
            {
                return Json(_service.GetWeeklySleepStats(await ResolveTargetUserIdAsync(), weekStart));
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"週次睡眠統計の取得中にエラーが発生しました。weekStart={weekStart:O}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
            }
        }

        /// <summary>
        /// 週次統計レポートを PDF として出力する。
        /// </summary>
        /// <remarks>
        /// サーバー側 Playwright で /Statistics?print=1 を内部レンダリングし PDF 化する。
        /// 認証クッキーは現リクエストのものを Playwright ブラウザコンテキストに転送する。
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> ExportPdf(DateTime? weekStart, string section = "weekly")
        {
            try
            {
                var isMonthly = section == "monthly";
                var wsDate = NormalizeWeekStart(weekStart);
                var ws = wsDate.ToString("yyyy-MM-dd");

                // ループバック用の内部URL。IIS in-process では IServerAddressesFeature が
                // ワイルドカードホスト（http://*:80 等）を返し Uri 解析に失敗するため、
                // また PathBase を含めないとサブアプリ配置で 404 になるため、現リクエストから組み立てる。
                var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
                var printSection = isMonthly ? "monthly" : "weekly";
                var url = $"{Request.Scheme}://{Request.Host}{pathBase}/Statistics?print=1&weekStart={ws}&section={printSection}";

                // 現リクエストの認証クッキーを Playwright 用に変換（ループバックなので Secure=false）
                var host = Request.Host.Host;
                var pwCookies = Request.Cookies.Select(c => new PWCookie
                {
                    Name = c.Key,
                    Value = c.Value,
                    Domain = host,
                    Path = "/",
                    Secure = false,
                    HttpOnly = true
                }).ToList();

                var pdfBytes = await _pdfExportService.RenderPdfAsync(url, pwCookies);

                // ファイル名にはデータ対象ユーザーの表示名を使う（Adminの場合は選択中Memberの名前）
                string userLabel;
                if (User.IsInRole("Admin"))
                {
                    var selectedId = await _userManagementService.GetSelectedMemberUserIdAsync();
                    var members = await _userManagementService.GetMemberListAsync();
                    userLabel = members.FirstOrDefault(m => m.Value == selectedId)?.Text ?? "user";
                }
                else
                {
                    userLabel = User.Identity?.Name ?? "user";
                }
                var safeUser = SanitizeFileNamePart(userLabel);
                var reportLabel = isMonthly ? "月次レポート" : "週次レポート";
                var period = isMonthly ? wsDate.ToString("yyyy-MM") : ws;
                var fileName = $"Phycock_{reportLabel}_{safeUser}_{period}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.Error(new LogModel($"PDF出力に失敗しました。weekStart={weekStart:O}, section={section}"), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "PDF出力に失敗しました。しばらく時間をおいてから再度お試しください。");
            }
        }

        /// <summary>ファイル名で使用できない文字を除去する。</summary>
        private static string SanitizeFileNamePart(string input)
        {
            if (string.IsNullOrEmpty(input)) return "user";
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(input.Where(c => !invalid.Contains(c) && c != ' ').ToArray());
            return string.IsNullOrEmpty(cleaned) ? "user" : cleaned;
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private async Task<string> ResolveTargetUserIdAsync()
            => User.IsInRole("Admin")
                ? await _userManagementService.GetSelectedMemberUserIdAsync()
                : GetCurrentUserId();
    }
}
