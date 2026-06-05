# PDF 出力: QuestPDF → Playwright 移行計画

**作成日**: 2026-05-12  
**対象フェーズ**: Phase 2  
**決定事項**: QuestPDF を完全削除し、Playwright headless print に一本化する

---

## 背景・決定理由

- QuestPDF Community ライセンスの商用利用可否が不明（プロジェクトの契約形態次第）
- Linux 本番（Azure App Service / Railway）でフォント豆腐が発生する
- 既存の HTML/Chart.js ベースの統計画面を帳票にそのまま使えるため二重管理が不要
- Chromium が日本語フォントを処理するため CJK 問題が消える

---

## スプリントコントラクト（完成条件）

**正常系:**
- [ ] 統計ページの「週次 PDF ダウンロード」ボタンを押すと、当該週の帳票 PDF がダウンロードされる
- [ ] 帳票に体調・気分折れ線グラフ、睡眠積み上げ棒グラフ、スケジュール帯、詳細テーブルが含まれる
- [ ] データなし日（体調記録 0 件）はグラフの線が途切れる（null 点を繋がない）
- [ ] フォントが日本語で正常に表示される（ローカル Windows / Linux 本番とも）
- [ ] PDF に認証が不要なエンドポイントへのアクセスは発生しない

**認可:**
- [ ] Member は自分の週次 PDF のみダウンロードできる
- [ ] Admin は選択中 Member の週次 PDF をダウンロードできる
- [ ] クエリ文字列で任意 userId を渡しても別ユーザーの PDF は出ない（IDOR ガード）

**非機能:**
- [ ] `Cache-Control: no-store, private` を PDF レスポンスに付与する
- [ ] `dotnet build` 警告 0 / エラー 0 が維持される
- [ ] `dotnet test` が全件合格する

---

## 削除対象

| 対象 | 備考 |
|---|---|
| `Phycock/Reports/WeeklyStatisticsReportDocument.cs` | QuestPDF 帳票クラス |
| `Phycock/Reports/WeeklyStatisticsReportModels.cs` | PDF 専用 DTO |
| `Phycock/Reports/WeeklyStatisticsReportSampleData.cs` | ダミーデータ（本体不要） |
| `tools/WeeklyStatisticsReportPdfSample/` | コンソール生成ツール |
| `Phycock.csproj` の QuestPDF 参照 | `PackageReference Include="QuestPDF"` を削除 |
| `docs/週次統計サンプル.pdf` | `.gitignore` 対象に追加して管理外へ |

---

## 追加・変更対象

### 1. NuGet パッケージ

```xml
<!-- Phycock.csproj に追加 -->
<PackageReference Include="Microsoft.Playwright" Version="1.*" />
```

Playwright は初回のみブラウザをインストールする必要がある。

```powershell
# セットアップコマンド（dotnet restore 後に一度だけ実行）
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

### 2. フォント

`wwwroot/fonts/NotoSansJP-Regular.woff2` を同梱する。

```powershell
# ダウンロードスクリプト（scripts/download-fonts.ps1 として作成）
Invoke-WebRequest -Uri "https://fonts.gstatic.com/s/notosansjp/..." `
    -OutFile "Phycock/wwwroot/fonts/NotoSansJP-Regular.woff2"
```

`_LayoutPrint.cshtml` で `@font-face` ローカル参照する。

### 3. 印刷レイアウト `Views/Shared/_LayoutPrint.cshtml`

- ナビゲーションバー・グローバルフッターなし
- `@font-face { font-family: 'Noto Sans JP'; src: url('/fonts/NotoSansJP-Regular.woff2'); }`
- A4 横レイアウト用 CSS（`@page { size: A4 landscape; margin: 10mm; }`）
- RenderBody のみ

### 4. 印刷ビュー `Views/Statistics/WeeklyPrint.cshtml`

`Layout = "_LayoutPrint"` を指定。

構成（QuestPDF 帳票と同等）:
- **ヘッダー**: タイトル・対象者・期間
- **グラフ**: Chart.js 睡眠積み上げ棒 + 体調・気分折れ線（`spanGaps: false` で null 点を切断）
- **スケジュール帯**: 日別カード（通所状態色分け）
- **詳細テーブル**: RecordTiming 別体調記録、Memo ありのみの睡眠記録

ViewModel: `WeeklyPrintViewModel`（HealthRecord + SleepRecord + ScheduleEntry を週単位で保持）

### 5. PDF 生成サービス `Service/PdfService.cs`

```csharp
public class PdfService
{
    public async Task<byte[]> GenerateFromHtmlAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });
        return await page.PdfAsync(new() { Format = "A4", Landscape = true, PrintBackground = true });
    }
}
```

`SetContentAsync` でサーバー側 HTML を直接渡すため、認証セッションが不要。

### 6. Razor → HTML 文字列変換

`IRazorViewEngine` + `ITempDataProvider` で `WeeklyPrint.cshtml` を HTML 文字列にレンダリングして `PdfService` に渡す。既存パターンがなければ `RazorViewToStringRenderer` をサービスとして実装する。

### 7. Controller action `StatisticsController.DownloadWeeklyPdf`

```csharp
[HttpGet]
public async Task<IActionResult> DownloadWeeklyPdf(DateTime weekStart)
{
    var userId = await ResolveTargetUserIdAsync(); // 既存メソッド流用
    var vm = await _service.GetWeeklyPrintDataAsync(userId, weekStart);
    var html = await _razorRenderer.RenderAsync("Statistics/WeeklyPrint", vm);
    var pdf = await _pdfService.GenerateFromHtmlAsync(html);

    Response.Headers["Cache-Control"] = "no-store, private";
    var filename = $"週次統計_{weekStart:yyyyMMdd}.pdf";
    return File(pdf, "application/pdf", filename);
}
```

### 8. StatisticsService 拡張

`GetWeeklyPrintDataAsync(string userId, DateTime weekStart)` を追加。
既存の `GetWeeklyHealthStats` / `GetWeeklySleepStats` のクエリを内部で統合し、`WeeklyPrintViewModel` を返す。

---

## 実装順序

```
1. 削除（QuestPDF 関連ファイル・参照）
2. NuGet 追加・ブラウザインストール
3. フォントダウンロードスクリプト作成＋同梱
4. _LayoutPrint.cshtml
5. WeeklyPrintViewModel + StatisticsService.GetWeeklyPrintDataAsync
6. WeeklyPrint.cshtml（Chart.js グラフ含む）
7. PdfService
8. RazorViewToStringRenderer
9. StatisticsController.DownloadWeeklyPdf
10. UI（ダウンロードボタン）
11. テスト追加
```

---

## テスト計画

| テスト | 観点 |
|---|---|
| `GetWeeklyPrintDataAsync` | 同日複数体調記録の平均、SleepType 別合計、Memo ありのみ抽出 |
| `DownloadWeeklyPdf` 権限 | Member が他ユーザー週 → 403 |
| PDF スモーク | バイト列が `%PDF` で始まること（Playwright 統合テスト） |

---

## 未解決・確認事項

- [ ] Azure / Railway のビルドパイプラインで `playwright install chromium` を実行するステップを追加する必要がある（CI 対応は本番デプロイ時に別途）
- [ ] `docs/週次統計サンプル.pdf` を `.gitignore` に追加してよいか（次セッションで対応）
