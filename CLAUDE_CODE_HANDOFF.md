# Phycock 共同開発ハンドオフ

最終更新: 2026-05-17
対象リポジトリ: `H:/ClaudeCode/Phycock`
status: active

このファイルは Codex と Claude Code の相互ハンドオフ log。書式・更新タイミングは `.claude/rules/handoff-protocol.md`、汎用ハーネスは `.claude/rules/cross-agent-harness.md`、Phycock 固有 profile は `.claude/rules/project-collaboration-profile.md` を参照。

新しいタスクは下記「進行中」にこのファイル最上部へ追記する（最新が上）。完了・マージ済みセクションは `docs/handoffs/archive/YYYY-QN.md` へ切り出す。

---

## 進行中

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
