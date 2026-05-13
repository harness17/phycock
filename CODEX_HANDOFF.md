# Phycock Codex Handoff

---

## 2026-05-12 PDF 出力方式変更決定

### Context

週次統計 PDF のレビューを実施し、QuestPDF から Playwright headless print への移行を決定した。
移行計画は `docs/plans/2026-05-12-pdf-playwright-migration.md` に記載。

### 決定事項

- QuestPDF を **完全削除**（`Phycock/Reports/`・`tools/WeeklyStatisticsReportPdfSample/`・`Phycock.csproj` 参照）
- 代替: Playwright で `SetContentAsync` → `PdfAsync`（認証セッション不要）
- 印刷専用レイアウト `_LayoutPrint.cshtml` + `Views/Statistics/WeeklyPrint.cshtml` を新設

### QuestPDF 廃止の根拠

| 問題 | 詳細 |
|---|---|
| ライセンス | Community ライセンスの商用利用可否が契約形態次第で不明 |
| フォント | Linux 本番（Azure/Railway）で CJK 豆腐が発生する |

### レビューで判明した他の修正事項（実装時に対応）

- `ConditionAverage ?? minScore` による null 点バグ → Chart.js `spanGaps: false` で解消
- PDF レスポンスに `Cache-Control: no-store, private` が必要（要配慮個人情報）
- `WeeklyStatisticsReportSampleData.cs` が本体 (`Phycock/`) に混入 → 削除対象

### 実装状況

- [x] レビュー実施・方針決定
- [x] 移行計画作成（`docs/plans/2026-05-12-pdf-playwright-migration.md`）
- [ ] 実装（次セッション以降）

### Next Action（実装着手時）

計画の実装順序に従う：

1. QuestPDF 関連ファイル・参照を削除
2. `Microsoft.Playwright` NuGet 追加・`playwright install chromium`
3. フォント（Noto Sans JP）ダウンロードスクリプト＋同梱
4. `_LayoutPrint.cshtml`
5. `WeeklyPrintViewModel` + `StatisticsService.GetWeeklyPrintDataAsync`
6. `WeeklyPrint.cshtml`（Chart.js、`spanGaps: false`）
7. `PdfService`（Playwright ラッパー）
8. `RazorViewToStringRenderer`
9. `StatisticsController.DownloadWeeklyPdf`
10. UI（ダウンロードボタン）
11. テスト追加

---

## 2026-05-12 週次統計PDFサンプル設計ライン（参照用・廃止予定）

### Context

Phycock の Phase 2 統計・出力仕様をリタリコ側に確認するため、週次統計レポートの画面サンプルと PDF サンプルを作成した。
→ **上記の方式変更決定により、QuestPDF 帳票は廃止。設計ラインの参照のみ残す。**

### Current Design Line（HTML 帳票に引き継ぐ内容）

- 週次統計は **A4横・1週間1ページ** を基本にする
- グラフ上段: 睡眠積み上げ棒（本睡眠・他睡眠）＋ 体調・気分折れ線
- 睡眠時間 6-8h の背景帯を目安として表示
- 中段: 通所スケジュールを日付ごとに並べる（複数予定対応）
- 下段: `RecordTiming` 別の体調記録＋ Memo ありのみの睡眠記録

### Review Points For LITALICO（未確認）

- A4横・1週間1ページで面談時に見やすいか
- 体調・気分の平均値でよいか、最大/最小/最新値も必要か
- 睡眠の正常目安を 6-8h としてよいか
- `本睡眠 / 他睡眠` の分類が現場の見方と合うか
- 通所状態の粒度（予定、通所済み、欠席、遅刻、早退）で足りるか

### Ownership

- Codex: 実装（計画の実装順序に従う）
- ClaudeCode: レビュー・設計判断・認可ロジック確認
