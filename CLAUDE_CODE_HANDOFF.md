# Phycock 共同開発ハンドオフ

最終更新: 2026-05-16
対象リポジトリ: `H:/ClaudeCode/Phycock`
status: active

このファイルは Codex と Claude Code の相互ハンドオフ log。書式・更新タイミングは `.claude/rules/handoff-protocol.md`、役割分担と merge ゲートは `.claude/rules/cross-agent-review.md` を参照。

既存の `CODEX_HANDOFF.md` は Codex 側の継続メモとして残し、相互依頼・レビュー・merge 判断はこのファイルに集約する。

---

## 2026-05-17 00:30 追記（統合カレンダー＋イベント並び順 — Codex 実装 / ClaudeCode 補完）

- 対象: current worktree
- 作成者: ClaudeCode（実装着手は ClaudeCode、テスト追加・View refine・Program.cs 調整は Codex。セクションは未作成だったため ClaudeCode が補完）
- 主題: 体調・睡眠・通所スケジュールを1枚に重ねる統合カレンダーを追加し、1日の流れ順（本睡眠→起床時→訓練開始時→通所予定→訓練終了時→就眠時、仮眠等は実時刻で挿入）に並び替える
- 変更ファイル:
  - 新規 `Phycock/Controllers/CalendarController.cs`
  - 新規 `Phycock/Views/Calendar/Index.cshtml`
  - `Phycock/Views/Shared/_Layout.cshtml`（ナビに「カレンダー」追加）
  - `Phycock/Models/HealthRecordViewModels.cs`（`CalendarEventExtendedProps.SortOrder` 追加。3種DTO共通基底）
  - `Phycock/Service/HealthRecordService.cs` / `SleepRecordService.cs` / `ScheduleEntryService.cs`（SortOrder 付与）
  - `Tests/HealthRecord` / `SleepRecord` / `ScheduleEntry` ServiceTests（SortOrder 検証 3 件）
  - `Phycock/Program.cs`（Development 限定の DB 初期化スキップ — ブラウザ smoke check 用）
- レビュー担当: ClaudeCode
- 触ってよい範囲: 上記カレンダー関連ファイル
- 触ってはいけない範囲: 既存3カレンダーページ（HealthRecord/SleepRecord/ScheduleEntry の各 Index）の挙動
- セルフ verify: ✅ `dotnet build` 0 error / `dotnet test` 94 passed
- 実動確認: ✅ ブラウザで 5/18 の並び順（本睡眠→起床時→訓練開始時→通所予定→訓練終了時→仮眠→就眠時）とチェックボックス切替を確認
- レビュー観点:
  - 訓練開始時／終了時がその日の実際の通所予定に連動しているか（固定時刻でないか）
  - 通所予定のない日のフォールバック値が妥当か
  - 既存3ページの挙動が変わっていないか

### 完成条件（スプリントコントラクト）

- 統合カレンダーで3種が色分け表示され、チェックボックスで表示切替できる。
- 並び順が本睡眠→起床時→訓練開始時→通所予定→訓練終了時→就眠時で固定され、仮眠等は実時刻で挿入される。
- 訓練開始時／終了時はその日の通所予定の開始／終了に連動する（通所予定12時終了なら14時の仮眠は訓練終了時より後）。
- `[Authorize]` 必須。Admin は既存 GetEvents の対象利用者判定に連動。
- 既存3ページの挙動・既存テストが回帰しない。

### レビュー結果（2026-05-17, ClaudeCode）

- 公開可否: 軽微指摘あり（コミット分離が必要。コード自体は良好）
- 重大指摘: なし
- 軽微指摘:
  - `Program.cs` の `SkipDatabaseInitialization` はカレンダー機能と無関係（smoke check 用インフラ）。コミットを分離する。
  - 作業ツリーに無関係な成果物（`docs/本社システム部門向け基本仕様書.*` ほか、`.claude/scheduled_tasks.lock`）が混在。カレンダー機能のコミットからは除外する。
  - クライアント側の並び替えロジック（`Index.cshtml` の `assignOrder`）は JS のため xUnit 非対象。手動ブラウザ確認で代替済み。自動テストは現状なし。
- 観点別判定: 動作=良好 / 契約=良好 / テスト=良好 / セキュリティ・監査=良好 / スタイル=軽微指摘

### Merge ゲート 4 条件

| 1 セルフ | 2 相互レビュー | 3 重大指摘 | 4 ユーザー指示 |
|----------|---------------|-----------|----------------|
| ✅ | ✅ | 残なし | ❌ 未指示 |

### 次アクション

- ユーザーが merge 方針を指示する。merge 時はカレンダー機能のファイルのみを個別指定でコミットし、ハーネス文書・無関係 docx は別コミットに分離する。

---

## 2026-05-16 21:00 追記（ClaudeCode 連携ハーネス導入 — Codex 作成）

- 対象: current worktree
- 作成者: Codex
- 主題: YouTom / 技術記事プロジェクトと同じ相互レビュー型ハーネスを Phycock 用に導入
- 変更ファイル:
  - `.claude/rules/cross-agent-review.md`
  - `.claude/rules/handoff-protocol.md`
  - `.claude/skills/codex-handoff/SKILL.md`
  - `.claude/skills/cross-review/SKILL.md`
  - `.agents/skills/implement-task/SKILL.md`
  - `AGENTS.md`
  - `CLAUDE.md`
  - `CLAUDE_CODE_HANDOFF.md`
- レビュー担当: ClaudeCode
- 触ってよい範囲: ハーネス文書・ルール・スキルのみ
- 触ってはいけない範囲: アプリ本体、既存未コミット変更
- セルフ verify: ✅ 文書構造と参照 grep 確認済み
- 実動確認: N/A（ドキュメントのみ）
- レビュー観点:
  - YouTom / 技術記事の共同開発ハーネスと同等の運用要素が入っているか
  - Phycock の ASP.NET Core MVC / EF Core / 認可 / 監査文脈に置き換わっているか
  - 既存 `CODEX_HANDOFF.md` と衝突せず、使い分けが明確か

### 完成条件（スプリントコントラクト）

- Claude Code が Codex へ実装依頼を作れる。
- Codex が handoff から実装・verify・handoff 更新へ進める。
- 反対側エージェントがレビュー結果を同じ handoff に残せる。
- Merge 前に build/test・相互レビュー・重大指摘なし・ユーザー指示の 4 条件を確認できる。
- アプリ本体の既存変更には触れない。

### 次アクション

- ClaudeCode が必要に応じて `/cross-review` 相当でハーネス内容を確認する。
