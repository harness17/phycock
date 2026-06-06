# Dashboard・TrendGraph・Heatmap 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 既存ダッシュボードにチェックリスト、統計ページに期間移動、カレンダーに体調ヒートマップを追加する

**Architecture:** 既存 DashboardService / StatisticsController / HealthRecordService を拡張する。新規 Service / ViewModel は作らない。FullCalendar の `dayCellDidMount` で日付セルを着色し、Chart.js は既存 CDN を流用する。DBスキーマ変更なし。

**Tech Stack:** ASP.NET Core 10 MVC, Entity Framework Core 10, Chart.js 4.4.7 (CDN済), FullCalendar 6.1.15 (CDN済), xUnit + Moq

---

## ファイル構成

| ファイル | 変更種別 | 責務 |
|---|---|---|
| `Phycock/Models/DashboardViewModel.cs` | 変更 | チェックリスト用プロパティ追加 |
| `Phycock/Service/DashboardService.cs` | 変更 | 睡眠記録チェック・最新体調取得を追加 |
| `Phycock/Controllers/HomeController.cs` | 変更 | エラーハンドリング追加 |
| `Phycock/Views/Home/Index.cshtml` | 変更 | チェックリスト UI 追加 |
| `Tests/Dashboard/DashboardServiceTests.cs` | 新規 | DashboardService のユニットテスト |
| `Phycock/Controllers/StatisticsController.cs` | 変更 | TrendData API 追加 |
| `Phycock/Views/Statistics/Index.cshtml` | 変更 | 前週/次週ボタン追加 |
| `Tests/Statistics/StatisticsControllerTests.cs` | 新規 | TrendData API のユニットテスト |
| `Phycock/Service/HealthRecordService.cs` | 変更 | GetHeatmapData メソッド追加 |
| `Phycock/Controllers/HealthRecordController.cs` | 変更 | HeatmapData API 追加 |
| `Phycock/Views/Calendar/Index.cshtml` | 変更 | dayCellDidMount + ヒートマップ fetch |
| `Tests/HealthRecord/HealthRecordServiceTests.cs` | 変更 | ヒートマップ集計テスト追加 |

---

### Task 1: DashboardViewModel にチェックリスト用プロパティを追加

**Files:**
- Modify: `Phycock/Models/DashboardViewModel.cs`

- [ ] **Step 1: DashboardViewModel にプロパティを追加**

```csharp
// Phycock/Models/DashboardViewModel.cs
using Phycock.Entity.Enums;

namespace Phycock.Models
{
    /// <summary>
    /// ダッシュボード表示 ViewModel。
    /// </summary>
    public class DashboardViewModel
    {
        /// <summary>今日の通所予定。</summary>
        public List<ScheduleEntryDetailDto> TodayScheduleEntries { get; set; } = new();

        /// <summary>今日の体調記録。</summary>
        public List<HealthRecordListViewModel> TodayHealthRecords { get; set; } = new();

        /// <summary>直近7日分の体調・睡眠サマリー。</summary>
        public WeeklySummaryDto WeeklySummary { get; set; } = new();

        // --- チェックリスト用プロパティ ---

        /// <summary>今日の睡眠記録があるかどうか。</summary>
        public bool HasSleepRecord { get; set; }

        /// <summary>直近の体調レベル（今日の最新記録）。記録なしは null。</summary>
        public ConditionLevel? LatestCondition { get; set; }

        /// <summary>直近の気分レベル（今日の最新記録）。記録なしは null。</summary>
        public FeelingLevel? LatestFeeling { get; set; }

        /// <summary>データ取得に失敗したかどうか。true の場合チェックリストは unavailable 表示。</summary>
        public bool IsUnavailable { get; set; }
    }

    /// <summary>
    /// 直近7日分の体調・睡眠サマリー DTO。
    /// </summary>
    public class WeeklySummaryDto
    {
        /// <summary>集計開始日。</summary>
        public DateTime StartDate { get; set; }

        /// <summary>集計終了日。</summary>
        public DateTime EndDate { get; set; }

        /// <summary>体調平均。</summary>
        public double? AverageCondition { get; set; }

        /// <summary>気分平均。</summary>
        public double? AverageFeeling { get; set; }

        /// <summary>睡眠時間合計。</summary>
        public TimeSpan TotalSleepDuration { get; set; }
    }
}
```

- [ ] **Step 2: ビルド確認**

Run: `dotnet build Phycock.slnx`
Expected: 0 errors（既存コードは新プロパティを使わないので影響なし）

- [ ] **Step 3: コミット**

```bash
git add Phycock/Models/DashboardViewModel.cs
git commit -m "feat: DashboardViewModel にチェックリスト用プロパティを追加"
```

---

### Task 2: DashboardService にチェックリスト集計ロジックを追加

**Files:**
- Modify: `Phycock/Service/DashboardService.cs`
- Create: `Tests/Dashboard/DashboardServiceTests.cs`

- [ ] **Step 1: テストファイルを作成**

```csharp
// Tests/Dashboard/DashboardServiceTests.cs
using Moq;
using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Repository;
using Phycock.Service;
using Xunit;

namespace Tests.Dashboard
{
    public class DashboardServiceTests
    {
        private readonly Mock<HealthRecordRepository> _healthRepo = new(null!);
        private readonly Mock<SleepRecordRepository> _sleepRepo = new(null!);
        private readonly Mock<ScheduleEntryRepository> _scheduleRepo = new(null!);

        private DashboardService CreateService()
        {
            var healthService = new HealthRecordService(_healthRepo.Object);
            var sleepService = new SleepRecordService(_sleepRepo.Object);
            var scheduleService = new ScheduleEntryService(_scheduleRepo.Object);
            return new DashboardService(healthService, sleepService, scheduleService);
        }

        [Fact]
        public void GetDashboard_WithHealthRecords_SetsLatestConditionAndFeeling()
        {
            var today = DateTime.Today;
            _healthRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<HealthRecordEntity>
                {
                    new() { UserId = "user-1", RecordDate = today, RecordTiming = RecordTiming.Morning,
                            Condition = ConditionLevel.Normal, Feeling = FeelingLevel.Good },
                    new() { UserId = "user-1", RecordDate = today, RecordTiming = RecordTiming.Evening,
                            Condition = ConditionLevel.Good, Feeling = FeelingLevel.VeryGood },
                });
            _healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<SleepRecordEntity>());
            _scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", DateOnly.FromDateTime(today)))
                .Returns(new List<ScheduleEntryEntity>());

            var result = CreateService().GetDashboard("user-1", false);

            Assert.Equal(2, result.TodayHealthRecords.Count);
            // 最新（Evening = 最後）の値が入る
            Assert.Equal(ConditionLevel.Good, result.LatestCondition);
            Assert.Equal(FeelingLevel.VeryGood, result.LatestFeeling);
        }

        [Fact]
        public void GetDashboard_WithNoHealthRecords_LatestConditionIsNull()
        {
            var today = DateTime.Today;
            _healthRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<HealthRecordEntity>());
            _healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<SleepRecordEntity>());
            _scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", DateOnly.FromDateTime(today)))
                .Returns(new List<ScheduleEntryEntity>());

            var result = CreateService().GetDashboard("user-1", false);

            Assert.Null(result.LatestCondition);
            Assert.Null(result.LatestFeeling);
        }

        [Fact]
        public void GetDashboard_WithSleepRecord_HasSleepRecordIsTrue()
        {
            var today = DateTime.Today;
            _healthRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<HealthRecordEntity>());
            _healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<SleepRecordEntity>
                {
                    new() { UserId = "user-1", RecordDate = today, StartDate = today.AddHours(-7), EndDate = today }
                });
            _sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            _scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", DateOnly.FromDateTime(today)))
                .Returns(new List<ScheduleEntryEntity>());

            var result = CreateService().GetDashboard("user-1", false);

            Assert.True(result.HasSleepRecord);
        }

        [Fact]
        public void GetDashboard_WithNoSleepRecord_HasSleepRecordIsFalse()
        {
            var today = DateTime.Today;
            _healthRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<HealthRecordEntity>());
            _healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<SleepRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            _scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", DateOnly.FromDateTime(today)))
                .Returns(new List<ScheduleEntryEntity>());

            var result = CreateService().GetDashboard("user-1", false);

            Assert.False(result.HasSleepRecord);
        }

        [Fact]
        public void GetDashboard_WithNoSchedule_TodayScheduleEntriesIsEmpty()
        {
            var today = DateTime.Today;
            _healthRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<HealthRecordEntity>());
            _healthRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<HealthRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndDate("user-1", today))
                .Returns(new List<SleepRecordEntity>());
            _sleepRepo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<SleepRecordEntity>());
            _scheduleRepo.Setup(x => x.GetByUserAndDate("user-1", DateOnly.FromDateTime(today)))
                .Returns(new List<ScheduleEntryEntity>());

            var result = CreateService().GetDashboard("user-1", false);

            Assert.Empty(result.TodayScheduleEntries);
            Assert.False(result.IsUnavailable);
        }
    }
}
```

- [ ] **Step 2: テストが失敗することを確認**

Run: `dotnet test Phycock.slnx --filter "DashboardServiceTests"`
Expected: FAIL（`HasSleepRecord` / `LatestCondition` が未設定なので一部テストが期待値と異なる）

- [ ] **Step 3: DashboardService にチェックリスト集計を実装**

```csharp
// Phycock/Service/DashboardService.cs
using Phycock.Models;

namespace Phycock.Service
{
    /// <summary>
    /// ダッシュボードサービス。
    /// </summary>
    public class DashboardService
    {
        private readonly HealthRecordService _healthRecordService;
        private readonly SleepRecordService _sleepRecordService;
        private readonly ScheduleEntryService _scheduleEntryService;

        public DashboardService(
            HealthRecordService healthRecordService,
            SleepRecordService sleepRecordService,
            ScheduleEntryService scheduleEntryService)
        {
            _healthRecordService = healthRecordService;
            _sleepRecordService = sleepRecordService;
            _scheduleEntryService = scheduleEntryService;
        }

        /// <summary>ダッシュボード表示データを取得する。</summary>
        public DashboardViewModel GetDashboard(string userId, bool isAdmin)
        {
            var weeklySummary = _healthRecordService.GetWeeklySummary(userId);
            weeklySummary.TotalSleepDuration = _sleepRecordService.GetSleepDuration(
                userId,
                weeklySummary.StartDate,
                weeklySummary.EndDate);

            var todayHealthRecords = _healthRecordService.GetTodaySummary(userId);
            var todaySleepRecords = _sleepRecordService.GetList(userId, DateTime.Today);

            // 最新の体調記録（RecordTiming の昇順で最後＝一番遅い時間帯）
            var latestHealth = todayHealthRecords.LastOrDefault();

            return new DashboardViewModel
            {
                TodayScheduleEntries = _scheduleEntryService.GetTodayEntries(userId),
                TodayHealthRecords = todayHealthRecords,
                WeeklySummary = weeklySummary,
                HasSleepRecord = todaySleepRecords.Count > 0,
                LatestCondition = latestHealth?.Condition,
                LatestFeeling = latestHealth?.Feeling,
            };
        }
    }
}
```

- [ ] **Step 4: テストが通ることを確認**

Run: `dotnet test Phycock.slnx --filter "DashboardServiceTests"`
Expected: 5 passed

- [ ] **Step 5: 全テスト回帰確認**

Run: `dotnet test Phycock.slnx`
Expected: All passed

- [ ] **Step 6: コミット**

```bash
git add Phycock/Service/DashboardService.cs Tests/Dashboard/DashboardServiceTests.cs
git commit -m "feat: DashboardService にチェックリスト集計ロジックを追加"
```

---

### Task 3: HomeController にエラーハンドリングを追加し、Home/Index にチェックリスト UI を追加

**Files:**
- Modify: `Phycock/Controllers/HomeController.cs`
- Modify: `Phycock/Views/Home/Index.cshtml`

- [ ] **Step 1: HomeController にエラーハンドリングを追加**

`HomeController.Index()` の `_dashboardService.GetDashboard()` 呼び出しを try-catch で囲み、例外時は `IsUnavailable = true` の ViewModel を返す:

```csharp
// HomeController.cs の Index メソッドを以下に変更
public async Task<IActionResult> Index()
{
    var userId = User.IsInRole("Admin")
        ? await _userManagementService.GetSelectedMemberUserIdAsync()
        : GetCurrentUserId();

    DashboardViewModel vm;
    if (string.IsNullOrWhiteSpace(userId))
    {
        vm = new DashboardViewModel();
    }
    else
    {
        try
        {
            vm = _dashboardService.GetDashboard(userId, User.IsInRole("Admin"));
        }
        catch
        {
            vm = new DashboardViewModel { IsUnavailable = true };
        }
    }

    return View(vm);
}
```

- [ ] **Step 2: Home/Index.cshtml にチェックリストセクションを追加**

既存の3カード（通所予定・今日の体調・直近7日サマリー）の上にチェックリストカードを挿入する。`<div class="row mt-4">` の直前に以下を追加:

```html
@* チェックリスト *@
<div class="card mb-4">
    <div class="card-body">
        <h5 class="card-title mb-3"><i class="fas fa-clipboard-check me-2"></i>今日の記録 <small class="text-muted">@DateTime.Today.ToString("yyyy/MM/dd (ddd)")</small></h5>
        @if (Model.IsUnavailable)
        {
            <div class="alert alert-warning mb-0">
                <i class="fas fa-exclamation-triangle me-1"></i>記録状況を取得できませんでした。しばらく後に再度お試しください。
            </div>
        }
        else
        {
            <ul class="list-group list-group-flush">
                <li class="list-group-item px-0 d-flex align-items-center">
                    @if (Model.TodayHealthRecords.Any())
                    {
                        <span class="text-success me-2"><i class="fas fa-check-circle"></i></span>
                        <span>体調記録 <span class="badge bg-success">@Model.TodayHealthRecords.Count 件</span></span>
                        <a asp-controller="HealthRecord" asp-action="Index" class="ms-auto btn btn-sm btn-outline-secondary">一覧</a>
                    }
                    else
                    {
                        <span class="text-muted me-2"><i class="far fa-circle"></i></span>
                        <span class="text-muted">体調記録 未記録</span>
                        <a asp-controller="HealthRecord" asp-action="Create" class="ms-auto btn btn-sm btn-primary">登録する</a>
                    }
                </li>
                <li class="list-group-item px-0 d-flex align-items-center">
                    @if (Model.HasSleepRecord)
                    {
                        <span class="text-success me-2"><i class="fas fa-check-circle"></i></span>
                        <span>睡眠記録 登録済み</span>
                    }
                    else
                    {
                        <span class="text-muted me-2"><i class="far fa-circle"></i></span>
                        <span class="text-muted">睡眠記録 未記録</span>
                        <a asp-controller="SleepRecord" asp-action="Create" class="ms-auto btn btn-sm btn-primary">登録する</a>
                    }
                </li>
                <li class="list-group-item px-0 d-flex align-items-center">
                    @if (Model.TodayScheduleEntries.Any())
                    {
                        <span class="text-success me-2"><i class="fas fa-check-circle"></i></span>
                        <span>通所予定 あり</span>
                    }
                    else
                    {
                        <span class="text-secondary me-2"><i class="fas fa-minus-circle"></i></span>
                        <span class="text-secondary">通所予定 なし（休日）</span>
                    }
                </li>
            </ul>
            @if (Model.LatestCondition.HasValue)
            {
                <div class="mt-3 text-muted small">
                    直近の体調: 体調「@Model.LatestCondition.Value.GetDisplayName()」 / 気分「@Model.LatestFeeling?.GetDisplayName()」
                </div>
            }
        }
    </div>
</div>
```

- [ ] **Step 3: ビルド確認**

Run: `dotnet build Phycock.slnx`
Expected: 0 errors

- [ ] **Step 4: 全テスト回帰確認**

Run: `dotnet test Phycock.slnx`
Expected: All passed

- [ ] **Step 5: コミット**

```bash
git add Phycock/Controllers/HomeController.cs Phycock/Views/Home/Index.cshtml
git commit -m "feat: ダッシュボードにチェックリスト UI を追加"
```

---

### Task 4: StatisticsController に TrendData API を追加

**Files:**
- Modify: `Phycock/Controllers/StatisticsController.cs`
- Create: `Tests/Statistics/StatisticsControllerTests.cs`

- [ ] **Step 1: テストファイルを作成**

```csharp
// Tests/Statistics/StatisticsControllerTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Phycock.Controllers;
using Phycock.Models;
using Phycock.Repository;
using Phycock.Service;
using System.Security.Claims;
using Xunit;

namespace Tests.Statistics
{
    public class StatisticsControllerTests
    {
        private static StatisticsController CreateController(string userId, string role = "Member")
        {
            var healthRepo = new Mock<HealthRecordRepository>(null!);
            healthRepo.Setup(x => x.GetByUserAndRange(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<Phycock.Entity.HealthRecordEntity>());
            var sleepRepo = new Mock<SleepRecordRepository>(null!);
            sleepRepo.Setup(x => x.GetByUserAndRange(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<Phycock.Entity.SleepRecordEntity>());
            var scheduleRepo = new Mock<ScheduleEntryRepository>(null!);
            scheduleRepo.Setup(x => x.GetByUserAndRange(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                .Returns(new List<Phycock.Entity.ScheduleEntryEntity>());

            var statisticsService = new StatisticsService(healthRepo.Object, sleepRepo.Object, scheduleRepo.Object);
            var userManagementService = new Mock<UserManagementService>(null!, null!, null!);
            userManagementService.Setup(x => x.GetSelectedMemberUserIdAsync()).ReturnsAsync(userId);
            var pdfExportService = new Mock<PdfExportService>(null!);
            var periodReflectionService = new Mock<PeriodReflectionService>(null!);

            var controller = new StatisticsController(
                statisticsService,
                userManagementService.Object,
                pdfExportService.Object,
                periodReflectionService.Object);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            return controller;
        }

        [Fact]
        public async Task TrendData_WithValidWeekStart_ReturnsJsonResult()
        {
            var controller = CreateController("user-1");
            var result = await controller.TrendData(DateTime.Today);

            Assert.IsType<JsonResult>(result);
        }

        [Fact]
        public async Task TrendData_WithWeekStartTooFarInPast_ReturnsBadRequest()
        {
            var controller = CreateController("user-1");
            var result = await controller.TrendData(DateTime.Today.AddDays(-400));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task TrendData_WithWeekStartTooFarInFuture_ReturnsBadRequest()
        {
            var controller = CreateController("user-1");
            var result = await controller.TrendData(DateTime.Today.AddDays(400));

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
```

- [ ] **Step 2: テストが失敗することを確認**

Run: `dotnet test Phycock.slnx --filter "StatisticsControllerTests"`
Expected: FAIL（`TrendData` メソッドが存在しない）

- [ ] **Step 3: StatisticsController に TrendData を実装**

`StatisticsController.cs` の `GetWeeklySleepStats` メソッドの後に以下を追加:

```csharp
/// <summary>
/// 週次トレンドデータを JSON で返す。前週/次週の期間移動 UI 用。
/// 既存 GetHealthWeekly + GetWeeklySleepStats を1回のリクエストにまとめた API。
/// </summary>
[HttpGet]
public async Task<IActionResult> TrendData(DateTime weekStart)
{
    try
    {
        var ws = NormalizeWeekStart(weekStart);
        var today = DateTime.Today;
        if (ws < today.AddDays(-365) || ws > today.AddDays(365))
        {
            return BadRequest(new { error = "weekStart は今日から ±365 日以内で指定してください。" });
        }

        var userId = await ResolveTargetUserIdAsync();
        var health = _service.GetWeeklyHealthStats(userId, ws);
        var sleep = _service.GetWeeklySleepStats(userId, ws);

        return Json(new
        {
            weekStart = ws.ToString("yyyy-MM-dd"),
            labels = health.Labels,
            condition = health.ConditionData,
            feeling = health.FeelingData,
            sleepHours = sleep.SleepHoursData,
        });
    }
    catch (Exception ex)
    {
        _logger.Error(new LogModel($"TrendData の取得中にエラーが発生しました。weekStart={weekStart:O}"), ex);
        return StatusCode(StatusCodes.Status500InternalServerError, new { error = DatabaseErrorMessage });
    }
}
```

- [ ] **Step 4: テストが通ることを確認**

Run: `dotnet test Phycock.slnx --filter "StatisticsControllerTests"`
Expected: 3 passed

- [ ] **Step 5: 全テスト回帰確認**

Run: `dotnet test Phycock.slnx`
Expected: All passed

- [ ] **Step 6: コミット**

```bash
git add Phycock/Controllers/StatisticsController.cs Tests/Statistics/StatisticsControllerTests.cs
git commit -m "feat: StatisticsController に TrendData API を追加"
```

---

### Task 5: Statistics ページに前週/次週ボタンを追加

**Files:**
- Modify: `Phycock/Views/Statistics/Index.cshtml`

- [ ] **Step 1: 週次タブ内のツールバー付近に前週/次週ボタンを追加**

既存の `<input id="weekStart" type="date">` と `<button id="reloadStats">` の間に前週/次週ボタンを挿入する。`Statistics/Index.cshtml` の `<div class="d-flex gap-2 align-items-center">` 内:

```html
<button id="prevWeek" type="button" class="btn btn-outline-secondary text-nowrap" title="前週">
    <i class="fas fa-chevron-left"></i>
</button>
<input id="weekStart" type="date" class="form-control" />
<button id="nextWeek" type="button" class="btn btn-outline-secondary text-nowrap" title="次週">
    <i class="fas fa-chevron-right"></i>
</button>
<button id="reloadStats" type="button" class="btn btn-outline-primary text-nowrap">更新</button>
```

- [ ] **Step 2: JavaScript に前週/次週ボタンのイベントハンドラを追加**

`@section Scripts` 内の `document.addEventListener('DOMContentLoaded', ...)` に以下を追加:

```javascript
document.getElementById('prevWeek')?.addEventListener('click', function () {
    const current = document.getElementById('weekStart').value;
    if (!current) return;
    const d = new Date(current);
    d.setDate(d.getDate() - 7);
    document.getElementById('weekStart').value = d.toISOString().substring(0, 10);
    document.getElementById('reloadStats').click();
});

document.getElementById('nextWeek')?.addEventListener('click', function () {
    const current = document.getElementById('weekStart').value;
    if (!current) return;
    const d = new Date(current);
    d.setDate(d.getDate() + 7);
    document.getElementById('weekStart').value = d.toISOString().substring(0, 10);
    document.getElementById('reloadStats').click();
});
```

- [ ] **Step 3: ビルド確認**

Run: `dotnet build Phycock.slnx`
Expected: 0 errors

- [ ] **Step 4: コミット**

```bash
git add Phycock/Views/Statistics/Index.cshtml
git commit -m "feat: 統計ページに前週/次週ボタンを追加"
```

---

### Task 6: HealthRecordService にヒートマップ日別集計メソッドを追加

**Files:**
- Modify: `Phycock/Service/HealthRecordService.cs`
- Modify: `Tests/HealthRecord/HealthRecordServiceTests.cs`

- [ ] **Step 1: テストを追加**

`Tests/HealthRecord/HealthRecordServiceTests.cs` に以下のテストを追加:

```csharp
[Fact]
public void GetHeatmapData_ReturnsMinConditionPerDay()
{
    var repo = new Mock<HealthRecordRepository>(null!);
    repo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
        .Returns(new List<HealthRecordEntity>
        {
            new() { UserId = "user-1", RecordDate = new DateTime(2026, 6, 1), Condition = ConditionLevel.Good, Feeling = FeelingLevel.Normal },
            new() { UserId = "user-1", RecordDate = new DateTime(2026, 6, 1), Condition = ConditionLevel.Bad, Feeling = FeelingLevel.Normal },
            new() { UserId = "user-1", RecordDate = new DateTime(2026, 6, 2), Condition = ConditionLevel.VeryGood, Feeling = FeelingLevel.Good },
        });
    var service = new HealthRecordService(repo.Object);

    var result = service.GetHeatmapData("user-1", new DateTime(2026, 6, 1), new DateTime(2026, 6, 7));

    Assert.Equal(2, result.Count);
    // 6/1 は Good(4) と Bad(2) → 最低値 Bad(2)
    Assert.Equal(2, result.First(x => x.Date == "2026-06-01").Level);
    // 6/2 は VeryGood(5) のみ
    Assert.Equal(5, result.First(x => x.Date == "2026-06-02").Level);
}

[Fact]
public void GetHeatmapData_NoRecords_ReturnsEmptyList()
{
    var repo = new Mock<HealthRecordRepository>(null!);
    repo.Setup(x => x.GetByUserAndRange("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
        .Returns(new List<HealthRecordEntity>());
    var service = new HealthRecordService(repo.Object);

    var result = service.GetHeatmapData("user-1", new DateTime(2026, 6, 1), new DateTime(2026, 6, 7));

    Assert.Empty(result);
}
```

- [ ] **Step 2: テストが失敗することを確認**

Run: `dotnet test Phycock.slnx --filter "GetHeatmapData"`
Expected: FAIL（`GetHeatmapData` が存在しない）

- [ ] **Step 3: HealthRecordService にメソッドと DTO を追加**

`HealthRecordService.cs` に以下を追加:

```csharp
/// <summary>ヒートマップ用の日別体調レベル集計を返す。各日の最低 ConditionLevel を代表値とする。</summary>
public List<HeatmapDayDto> GetHeatmapData(string userId, DateTime startDate, DateTime endDate)
{
    if (string.IsNullOrWhiteSpace(userId) || endDate <= startDate) return new List<HeatmapDayDto>();

    return _repository.GetByUserAndRange(userId, startDate, endDate.AddDays(-1))
        .GroupBy(x => x.RecordDate.Date)
        .Select(g => new HeatmapDayDto
        {
            Date = g.Key.ToString("yyyy-MM-dd"),
            Level = (int)g.Min(x => x.Condition),
        })
        .OrderBy(x => x.Date)
        .ToList();
}
```

`Phycock/Models/` に `HeatmapDayDto.cs` を追加するか、`HealthRecordService.cs` 末尾に内部クラスとして置く。ここではモデルファイルに追加する:

```csharp
// Models/HeatmapDayDto.cs（または既存の HealthRecord モデルファイルに追記）
namespace Phycock.Models
{
    /// <summary>ヒートマップ日別データ。</summary>
    public class HeatmapDayDto
    {
        /// <summary>日付（yyyy-MM-dd 形式）。</summary>
        public string Date { get; set; } = "";

        /// <summary>体調レベル（ConditionLevel の int 値）。その日の最低値。</summary>
        public int Level { get; set; }
    }
}
```

- [ ] **Step 4: テストが通ることを確認**

Run: `dotnet test Phycock.slnx --filter "GetHeatmapData"`
Expected: 2 passed

- [ ] **Step 5: 全テスト回帰確認**

Run: `dotnet test Phycock.slnx`
Expected: All passed

- [ ] **Step 6: コミット**

```bash
git add Phycock/Service/HealthRecordService.cs Phycock/Models/HeatmapDayDto.cs Tests/HealthRecord/HealthRecordServiceTests.cs
git commit -m "feat: HealthRecordService にヒートマップ日別集計メソッドを追加"
```

---

### Task 7: HealthRecordController に HeatmapData API を追加

**Files:**
- Modify: `Phycock/Controllers/HealthRecordController.cs`

- [ ] **Step 1: HealthRecordController に HeatmapData アクションを追加**

`GetEvents` メソッドの後に以下を追加:

```csharp
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
```

- [ ] **Step 2: ビルド確認**

Run: `dotnet build Phycock.slnx`
Expected: 0 errors

- [ ] **Step 3: 全テスト回帰確認**

Run: `dotnet test Phycock.slnx`
Expected: All passed

- [ ] **Step 4: コミット**

```bash
git add Phycock/Controllers/HealthRecordController.cs
git commit -m "feat: HealthRecordController に HeatmapData API を追加"
```

---

### Task 8: カレンダーに体調ヒートマップ（dayCellDidMount）を追加

**Files:**
- Modify: `Phycock/Views/Calendar/Index.cshtml`

- [ ] **Step 1: ヒートマップ用の色マッピングと fetch ロジックを追加**

`Calendar/Index.cshtml` の `<script>` 内、`calendarSources` 定義の後に以下を追加:

```javascript
// ヒートマップ色マッピング（半透明）
const heatmapColors = {
    1: 'rgba(239, 83, 80, 0.2)',   // VeryBad
    2: 'rgba(255, 152, 0, 0.2)',   // Bad
    3: 'rgba(255, 238, 88, 0.2)',  // Normal
    4: 'rgba(156, 204, 101, 0.2)', // Good
    5: 'rgba(102, 187, 106, 0.2)', // VeryGood
};

let heatmapData = {};

function fetchHeatmap(start, end) {
    const url = '@Url.Action("HeatmapData", "HealthRecord")';
    $.get(url, { start: start, end: end }).done(function (data) {
        heatmapData = {};
        (data || []).forEach(function (d) { heatmapData[d.date] = d.level; });
        // セル再描画のためカレンダーを再レンダリング
        if (window._phycockCalendar) {
            window._phycockCalendar.render();
        }
    });
}
```

- [ ] **Step 2: FullCalendar 初期化に dayCellDidMount と datesSet を追加**

`new FullCalendar.Calendar(...)` のオプションに以下を追加:

```javascript
dayCellDidMount: function (info) {
    if (!document.getElementById('toggleHealth').checked) return;
    const key = info.date.toISOString().substring(0, 10);
    const level = heatmapData[key];
    if (level && heatmapColors[level]) {
        info.el.style.backgroundColor = heatmapColors[level];
    }
},
datesSet: function (info) {
    fetchHeatmap(info.startStr, info.endStr);
},
```

- [ ] **Step 3: カレンダー変数をグローバルに保持**

`calendar.render();` の直前に以下を追加:

```javascript
window._phycockCalendar = calendar;
```

- [ ] **Step 4: トグル連動 — 体調トグル OFF 時にヒートマップもクリア**

既存の `checkbox.addEventListener('change', ...)` を以下に拡張:

```javascript
document.querySelectorAll('.form-check-input[data-source]').forEach(function (checkbox) {
    checkbox.addEventListener('change', function () {
        calendar.refetchEvents();
        // 体調トグル OFF → ヒートマップもクリア
        if (checkbox.dataset.source === 'health' && !checkbox.checked) {
            heatmapData = {};
        }
        calendar.render();
    });
});
```

- [ ] **Step 5: ビルド確認**

Run: `dotnet build Phycock.slnx`
Expected: 0 errors

- [ ] **Step 6: コミット**

```bash
git add Phycock/Views/Calendar/Index.cshtml
git commit -m "feat: カレンダーに体調ヒートマップ（dayCellDidMount）を追加"
```

---

### Task 9: ブラウザ動作確認

**Files:** なし（検証のみ）

- [ ] **Step 1: アプリを起動**

Run: `dotnet run --project Phycock`
Expected: `http://localhost:5000` で起動

- [ ] **Step 2: ダッシュボードのチェックリストを確認**

`http://localhost:5000` にアクセスし、以下を確認:
- チェックリストカードが表示される
- 体調記録/睡眠記録/通所予定の状態が正しい
- 未記録項目に「登録する」リンクがある
- 通所予定なし＝「休日」と表示される
- 既存の3カード（通所予定・今日の体調・直近7日サマリー）が回帰していない

- [ ] **Step 3: 統計ページの前週/次週ボタンを確認**

`http://localhost:5000/Statistics` にアクセスし、以下を確認:
- 前週/次週ボタンが表示される
- クリックするとグラフが更新される
- 既存のグラフ・テーブルが回帰していない

- [ ] **Step 4: カレンダーのヒートマップを確認**

`http://localhost:5000/Calendar` にアクセスし、以下を確認:
- 体調記録がある日のセル背景に半透明の色が付いている
- 体調レベルに応じた色（赤〜緑）が正しい
- 月移動すると色が更新される
- 体調トグル OFF でヒートマップが消える
- イベント文字・日付文字のコントラストが維持されている

- [ ] **Step 5: Admin でも同様の確認**

`admin1@sample.jp` / `Admin1!` でログインし、Member 切替後に各機能が対象 Member のデータを表示するか確認

---

### Task 10: 最終全テスト確認とコミット

- [ ] **Step 1: 全テスト確認**

Run: `dotnet test Phycock.slnx`
Expected: All passed

- [ ] **Step 2: 残りの未コミットファイルがないか確認**

Run: `git status --short`
Expected: nothing to commit（全ステップで都度コミット済み）
