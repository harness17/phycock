using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Phycock.Controllers
{
    /// <summary>
    /// 体調・睡眠・通所スケジュールを1枚に重ね表示する統合カレンダーコントローラー。
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class CalendarController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();
    }
}
