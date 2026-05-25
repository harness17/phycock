# Phycock 共同開発ハンドオフ

最終更新: 2026-05-22
対象リポジトリ: `H:/ClaudeCode/Phycock`
status: active

このファイルは Codex と Claude Code の相互ハンドオフ log。書式・更新タイミングは `.claude/rules/handoff-protocol.md`、汎用ハーネスは `.claude/rules/cross-agent-harness.md`、Phycock 固有 profile は `.claude/rules/project-collaboration-profile.md` を参照。

新しいタスクは下記「進行中」にこのファイル最上部へ追記する（最新が上）。完了・マージ済みセクションは `docs/handoffs/archive/YYYY-QN.md` へ切り出す。

---

## 進行中

### 2026-05-25 14:30 月次タブで「更新」押下時に月次タブを維持する（Claude Code → Codex 依頼）

- 対象: `Phycock/Views/Statistics/Index.cshtml`（および必要なら `Phycock/Controllers/StatisticsController.cs`）
- 作成者: Claude Code
- 主題: 統計画面で月次タブ表示中にカレンダー（日付 input）を変更して「更新」を押すと、現状は常に週次タブに戻ってしまう。月次表示中に更新した場合は該当月の月次タブが選択された状態で再表示してほしい。
- 触ってよい範囲:
  - `Phycock/Views/Statistics/Index.cshtml` の `#reloadStats` クリックハンドラ（行 947-949 付近）
  - 同 View の `nav-tabs` 初期 active 状態判定
  - 必要なら `Phycock/Controllers/StatisticsController.cs` の `Index(weekStart)` に `tab` クエリ追加
- 触ってはいけない範囲:
  - 所感（PeriodReflection）まわりの新規ファイル群（`_PeriodReflectionDisplay.cshtml` / `PeriodReflectionController` / `PeriodReflectionService` / Entity / migration）。今回の依頼に無関係なので変更しない。
  - PDF出力 (`ExportPdf` / Playwright PDF) のロジック本体。所感や改ページ CSS の挙動は壊さない。
  - 通所セルの「予定→振り返り」表示（`ScheduleRows`）まわり。
- 完成条件（正常系・異常系・回帰チェック）:
  - 週次タブ表示中に日付を変更して「更新」を押すと、従来通り週次タブで該当週が表示される。
  - 月次タブ表示中に日付を変更して「更新」を押すと、月次タブが選択された状態で **該当日付を含む月** の月次カレンダーが表示される。
  - PDF出力ボタン（`#exportPdf`）の遷移先指定（`section=weekly|monthly`）は現状の挙動を維持する。
  - URL を `/Statistics?weekStart=YYYY-MM-DD&tab=monthly` のように直接叩いた場合も、月次タブが active で開く。
  - `tab` クエリが無い／不正な値の場合は週次タブ（既存デフォルト）で開く。
  - 既存の `dotnet build Phycock.slnx` / `dotnet test Phycock.slnx`（106 passed）が通る。
- 実装ヒント（強制ではない）:
  - シンプル案: `#reloadStats` クリック時に `document.querySelector('.nav-tabs .nav-link.active').dataset.bsTarget` から現在タブ（`#weekly` or `#monthly`）を判別し、URL に `&tab=weekly|monthly` を付ける。
  - View 側 `@{}` ブロックで `Context.Request.Query["tab"]` を読み取り、`active`/`show active` の付与先を分岐する。
  - Bootstrap5 では JS なしでタブの初期 active は class で決まる。サーバー側で active を切り替えるのが最も素直。
- 注意:
  - 月次タブの所感（`#monthlyReflectionContainer`）は非同期更新で innerHTML 置換しているため、初期 active が monthly の時に表示崩れないか軽くチェック。
  - 所感の「更新」ボタンとは別物（`#reloadStats` は画面右上の「更新」、所感保存は offcanvas 内の保存ボタン）。混同しない。
- セルフ verify: 実装後に `dotnet build` / `dotnet test`、`dotnet run` でブラウザ手動確認（週次→更新で週次、月次→更新で月次、PDF出力で section=monthly/weekly が正しく分岐）。
- 次アクション: Codex 実装 → Claude Code が `/cross-review` でレビュー → ユーザー指示でマージ判断。

### 2026-05-22 体調レポート分析フィードバック（Codex 分析、実装候補）

- 追加資料: `docs/plans/2026-05-22-health-report-analysis-feedback.md`
- 入力: `R.H` の 2026年5月 月次PDF、週次PDF（2026-05-03 / 2026-05-10 / 2026-05-17 週）
- 分析概要: 2026-05-02〜2026-05-22 の21日分では、体調平均 2.68、気分平均 2.82、睡眠平均 8.95h。5/17週は体調・気分とも下振れし、5/20〜5/22 は低気圧、悪夢、集中力低下、疲労、体の重さが重なった低調期として読める。
- プロダクト示唆: Phycock 統計は診断ではなく、就労準備の自己観察・支援員面談の説明材料として価値がある。月次レポートには 7日平均、低調日数・連続低調日数、症状キーワード集計、通所/在宅別傾向を追加候補にする。
- 注意点: 睡眠9h超の翌日体調が低い傾向は、長時間睡眠が原因とは断定しない。不調時の回復睡眠・仮眠が混じる可能性として扱う。
- 次アクション: 月次レポート改善を行う場合、上記追加資料の「受け入れ条件案」を completion criteria として、`StatisticsService` の日別集計を再利用する。

### 2026-05-20 週次・月次レポート 睡眠時間軸 12h 化（Codex 実装、レビュー待ち）

- 変更範囲: `Phycock/Views/Statistics/Index.cshtml`
- 実装概要: 週次・月次の体調・気分・睡眠複合チャートで共有している `reportChartConfig` の睡眠時間 Y 軸上限を 10h から 12h に変更。画面表示と PDF 用 print 表示は同じ設定を使う。
- verify: `dotnet build Phycock.slnx` 成功（0 warnings）。`dotnet test Phycock.slnx` 成功（106 passed）。
- 残リスク: ブラウザ目視確認は未実施。Claude Code 側で週次・月次タブまたは PDF 出力時に睡眠軸が 12h まで表示されることを確認するとよい。

### 2026-05-20 PDF出力 ローディング表示（Codex 実装、レビュー待ち）

- 変更範囲: `Phycock/Views/Statistics/Index.cshtml`
- 実装概要: 統計画面の PDF 出力ボタンにスピナー付きの「作成中...」表示を追加。クリック後はボタンを disabled / `aria-busy` にし、`fetch` で PDF を取得して Blob ダウンロードすることで完了・失敗時に表示を戻す。PDF 以外のレスポンスは保存せず、ログイン状態確認のエラーとして扱う。
- verify: `dotnet build Phycock.slnx` 成功（0 warnings）。`dotnet test Phycock.slnx` 成功（106 passed）。
- 実動確認: `agent-browser` が PATH になくブラウザ目視確認は未実施。Razor 構文と JS 差分は確認済み。
- 残リスク: 実ブラウザでのダウンロード完了挙動、ファイル名復元、ボタン幅の見え方は Claude Code 側で目視確認するとよい。
- 次アクション: Claude Code レビューで統計画面から週次/月次 PDF を1回ずつ出力し、ローディング表示と二重押下防止を確認する。

### 2026-05-20 体調記録 任意時刻対応（Codex 実装、レビュー待ち）

- 変更範囲: `HealthRecord` entity/viewmodel/service/repository/controller/view、`StatisticsService`/統計View、EF migration、関連 xUnit。
- 実装概要: `RecordTiming.Custom` と `HealthRecord.RecordTime` を追加。定時タイミングは同日1件、任意時刻は同日同時刻のみ重複禁止にした。カレンダーイベントは任意時刻のみ `start` を日時化し、週次/月次統計にも任意時刻ラベルを表示する。
- DB: `20260520012148_AddHealthRecordCustomTime` で `RecordTime time null` を追加し、一意 index を `(UserId, RecordDate, RecordTiming, RecordTime)` に変更。
- verify: `dotnet test Phycock.slnx` 成功（107 passed / 0 warnings）。
- 実動確認: `member1@sample.jp` に 2026-05-20 の定時1件、任意時刻 10:15/14:35 を投入。ログイン後 `HealthRecord/GetEvents` が時刻付きイベントを返し、`Statistics?weekStart=2026-05-17` の月次セルに任意時刻要約が出ることを確認。
- 残リスク: ブラウザの視覚スクリーンショット確認は未実施。Claude Code 側でフォームの表示切替とカレンダー実描画を目視確認するとよい。
- 次アクション: Claude Code レビュー後、問題なければ migration 適用前提で通常フローに戻す。

---

## アーカイブ済み

### 2026-Q2 — [docs/handoffs/archive/2026-Q2.md](docs/handoffs/archive/2026-Q2.md)

- 2026-05-17 月次統計カレンダー（Codex 実装、マージ済み）
- 2026-05-17 統合カレンダー＋イベント並び順（Codex/ClaudeCode、マージ済み）
- 2026-05-17 共同開発ハーネスの汎用化（Codex、マージ済み）
- 2026-05-16 ClaudeCode 連携ハーネス導入（Codex、マージ済み）
- 2026-05-12 PDF 出力方式変更決定 — QuestPDF→Playwright 移行（旧 `CODEX_HANDOFF.md`、移行完了）
- 2026-05-12 週次統計PDFサンプル設計ライン（参照用、HTML 帳票に引き継ぎ済み）
