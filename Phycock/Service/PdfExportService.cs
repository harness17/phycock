using Microsoft.Playwright;

namespace Phycock.Service
{
    /// <summary>
    /// サーバー側 Playwright で内部URLをレンダリングし、PDFバイト列を返す。
    /// </summary>
    /// <remarks>
    /// IPlaywright は Singleton（IPlaywrightFactory で初期化）、Browser は per-request 起動。
    /// 認証クッキーは Controller 側で取得して渡す。
    /// </remarks>
    public class PdfExportService
    {
        private readonly IPlaywrightFactory _playwrightFactory;
        private readonly ILogger<PdfExportService> _logger;

        public PdfExportService(
            IPlaywrightFactory playwrightFactory,
            ILogger<PdfExportService> logger)
        {
            _playwrightFactory = playwrightFactory;
            _logger = logger;
        }

        /// <summary>
        /// 指定URLをレンダリングし、PDFバイト列を返す。
        /// </summary>
        /// <param name="url">フルURL（例: http://localhost:5232/Statistics?print=1&amp;weekStart=2026-05-04）</param>
        /// <param name="cookies">認証用クッキー（current request の Request.Cookies から構築）</param>
        /// <param name="readyFlagJs">描画完了判定の JS 式（true 評価で完了とみなす）</param>
        /// <param name="timeoutMs">全体タイムアウト</param>
        public async Task<byte[]> RenderPdfAsync(
            string url,
            IEnumerable<Cookie> cookies,
            string readyFlagJs = "() => window.chartsReady === true",
            int timeoutMs = 30000)
        {
            var playwright = await _playwrightFactory.GetAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
            });

            // ポイント: ループバック http アクセスのため Secure=false で渡す（元クッキーが Secure 付きでも上書き）
            await context.AddCookiesAsync(cookies.Select(c => new Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path ?? "/",
                Secure = false,
                HttpOnly = c.HttpOnly,
                SameSite = SameSiteAttribute.Lax
            }));

            var page = await context.NewPageAsync();

            try
            {
                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = timeoutMs
                });

                if (response is null || !response.Ok)
                {
                    var status = response?.Status.ToString() ?? "no response";
                    _logger.LogError("PDF print page returned status {Status} for url {Url}", status, url);
                    throw new InvalidOperationException($"印刷ページの取得に失敗しました (status={status})");
                }

                // ポイント: Chart.js のアニメーション完了 + 画像化を window.chartsReady で待機
                await page.WaitForFunctionAsync(readyFlagJs, null, new PageWaitForFunctionOptions
                {
                    Timeout = timeoutMs
                });

                var pdfBytes = await page.PdfAsync(new PagePdfOptions
                {
                    Format = "A4",
                    Landscape = true,
                    PrintBackground = true,
                    Margin = new Margin { Top = "8mm", Bottom = "8mm", Left = "8mm", Right = "8mm" }
                });

                return pdfBytes;
            }
            finally
            {
                await context.CloseAsync();
            }
        }
    }

    /// <summary>
    /// IPlaywright を Singleton で保持するファクトリ。
    /// </summary>
    public interface IPlaywrightFactory
    {
        Task<IPlaywright> GetAsync();
    }

    /// <inheritdoc />
    public class PlaywrightFactory : IPlaywrightFactory, IAsyncDisposable
    {
        private IPlaywright? _instance;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public async Task<IPlaywright> GetAsync()
        {
            if (_instance is not null) return _instance;
            await _lock.WaitAsync();
            try
            {
                _instance ??= await Playwright.CreateAsync();
                return _instance;
            }
            finally
            {
                _lock.Release();
            }
        }

        public ValueTask DisposeAsync()
        {
            _instance?.Dispose();
            _instance = null;
            _lock.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
