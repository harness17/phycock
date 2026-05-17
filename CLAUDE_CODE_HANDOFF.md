# Phycock 共同開発ハンドオフ

最終更新: 2026-05-16
対象リポジトリ: `H:/ClaudeCode/Phycock`
status: active

このファイルは Codex と Claude Code の相互ハンドオフ log。書式・更新タイミングは `.claude/rules/handoff-protocol.md`、汎用ハーネスは `.claude/rules/cross-agent-harness.md`、Phycock 固有 profile は `.claude/rules/project-collaboration-profile.md` を参照。

既存の `CODEX_HANDOFF.md` は Codex 側の継続メモとして残し、相互依頼・レビュー・merge 判断はこのファイルに集約する。

---

## 2026-05-17 追記（月次統計カレンダー — Codex 実装）

- 対象: `codex-schedule-activity-types`
- 作成者: Codex
- 主題: 統計ページの月次タブに、体調平均・気分平均・睡眠時間合計・通所予定を日別表示する月グリッドカレンダーを追加
- 変更ファイル:
  - 新規 `Phycock/Common/SleepStandards.cs`
  - `Phycock/Models/StatisticsViewModels.cs`
  - `Phycock/Service/StatisticsService.cs`
  - `Phycock/Controllers/StatisticsController.cs`
  - `Phycock/Views/Statistics/Index.cshtml`
- レビュー担当: ClaudeCode
- 触ってよい範囲: 上記統計関連ファイル
- 触ってはいけない範囲: エンティティ、リポジトリ、マイグレーション、他コントローラー
- セルフ verify:
  - ⚠️ `dotnet build Phycock.slnx` は sandbox から `C:\Users\harne\AppData\Roaming\NuGet\NuGet.Config` を読めず失敗
  - ✅ `dotnet build Phycock.slnx --no-restore` 0 error
  - ⚠️ `dotnet test Phycock.slnx` は同じ NuGet.Config 権限で失敗
  - ✅ `dotnet test Phycock.slnx --no-restore` 94 passed
- 実動確認:
  - ⚠️ in-app browser は Node REPL 実行ツールが露出しておらず未実施
  - ⚠️ 一時 `dotnet run` のバックグラウンド起動はローカルポリシーでブロックされたため未実施
- レビュー観点:
  - 月次カレンダーのグリッド開始・終了が日曜開始/土曜終了になっているか
  - `BuildDailyReport` 再利用により週次レポートと体調平均・睡眠重なり・通所判定が一致しているか
  - 睡眠分類が `<6` / `6-7` / `7-9` / `>9` に統一され、週次チャート目安帯も 7-9h に変わっているか
  - Admin/Member の対象ユーザー解決が既存 `ResolveTargetUserIdAsync` のまま維持されているか

### 完成条件（スプリントコントラクト）

- 月次タブで当月のカレンダーグリッド（日曜開始）が表示される。
- 当月の日だけ体調・気分・睡眠・通所の色が付き、前後月セルは薄い日付のみになる。
- 睡眠時間が 5h/6.5h/8h/10h の場合に、それぞれ不足/やや不足/適正/過剰の色へ分類される。
- Admin は選択中 Member、Member は自分のデータのみ表示する既存ルートを流用する。
- 記録ゼロ月でもグリッドが崩れず、記録なしは灰表示になる。
- 週次レポート・既存月次チャート・PDF 出力の既存ルートを壊さない。

### 次アクション

- ClaudeCode が UI 実動確認とレビューを行う。

---

## 2026-05-17 追記（月次統計カレンダー — Codex 実装）

- 対象: `codex-schedule-activity-types`
- 作成者: Codex
- 主題: 統計ページの月次タブに、体調平均・気分平均・睡眠時間合計・通所予定を日別表示する月グリッドカレンダーを追加
- 変更ファイル:
  - 新規 `Phycock/Common/SleepStandards.cs`
  - `Phycock/Models/StatisticsViewModels.cs`
  - `Phycock/Service/StatisticsService.cs`
  - `Phycock/Controllers/StatisticsController.cs`
  - `Phycock/Views/Statistics/Index.cshtml`
- レビュー担当: ClaudeCode
- 触ってよい範囲: 上記統計関連ファイル
- 触ってはいけない範囲: エンティティ、リポジトリ、マイグレーション、他コントローラー
- セルフ verify: ✅ `dotnet build Phycock.slnx` 0 error / ✅ `dotnet test Phycock.slnx` 94 passed
- 実動確認: ⚠️ in-app browser は Node REPL 実行ツールが露出しておらず未実施。ローカルサーバーのバックグラウンド起動も環境ポリシーでブロック。
- レビュー観点:
  - 月次カレンダーのグリッド開始・終了が日曜開始/土曜終了になっているか
  - `BuildDailyReport` 再利用により週次レポートと体調平均・睡眠重なり・通所判定が一致しているか
  - 睡眠分類が `<6` / `6-7` / `7-9` / `>9` に統一され、週次チャート目安帯も 7-9h に変わっているか
  - Admin/Member の対象ユーザー解決が既存 `ResolveTargetUserIdAsync` のまま維持されているか

### 完成条件（スプリントコントラクト）

- 月次タブで当月のカレンダーグリッド（日曜開始）が表示される。
- 当月の日だけ体調・気分・睡眠・通所の色が付き、前後月セルは薄い日付のみになる。
- 睡眠時間が 5h/6.5h/8h/10h の場合に、それぞれ不足/やや不足/適正/過剰の色へ分類される。
- Admin は選択中 Member、Member は自分のデータのみ表示する既存ルートを流用する。
- 記録ゼロ月でもグリッドが崩れず、記録なしは灰表示になる。
- 週次レポート・既存月次チャート・PDF 出力の既存ルートを壊さない。

### 次アクション

- ClaudeCode が UI 実動確認とレビューを行う。

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

## 2026-05-17 00:45 追記（共同開発ハーネスの汎用化 — Codex 作成）

- 対象: current worktree
- 作成者: Codex
- 主題: Phycock 直書きだった共同開発ハーネスを、汎用本体と Phycock profile に分離
- 変更ファイル:
  - `.claude/rules/cross-agent-harness.md`
  - `.claude/rules/project-collaboration-profile.md`
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
- セルフ verify: ✅ 旧参照 grep と汎用本体の Phycock 固有語混入 grep を確認済み
- 実動確認: N/A（ドキュメントのみ）
- レビュー観点:
  - `cross-agent-harness.md` がプロジェクト非依存の本体として使えるか
  - Phycock 固有情報が `project-collaboration-profile.md` に分離されているか
  - 各 skill が hard-coded な Phycock 前提ではなく、project profile を参照しているか

### 完成条件（スプリントコントラクト）

- 汎用本体は別プロジェクトへコピーしても使える。
- プロジェクト固有の担当境界・verify・重大指摘は profile に閉じている。
- Phycock の `AGENTS.md` / `CLAUDE.md` は汎用本体と profile の両方へ誘導する。
- 既存 handoff の意味を壊さない。
- アプリ本体の既存変更には触れない。

### 次アクション

- ClaudeCode が必要に応じて `/cross-review` 相当で、汎用本体と Phycock profile の分離が妥当か確認する。

### 試運転結果（2026-05-17, Codex）

- `cross-agent-harness.md` と `project-collaboration-profile.md` を読み込み、汎用本体と Phycock 固有 profile の分離を確認。
- `rg` で `AGENTS.md` / `CLAUDE.md` / `.claude` / `.agents` / `CLAUDE_CODE_HANDOFF.md` の参照を確認し、旧 `cross-agent-review.md` への有効参照は残っていない。
- `codex-handoff` / `cross-review` / `implement-task` は project profile を優先する導線になっている。
- アプリ本体変更はなし。ドキュメントのみのため `dotnet build/test` は実行対象外。

---

## 2026-05-16 21:00 追記（ClaudeCode 連携ハーネス導入 — Codex 作成）

- 対象: current worktree
- 作成者: Codex
- 主題: YouTom / 技術記事プロジェクトと同じ相互レビュー型ハーネスを Phycock 用に導入
- 変更ファイル:
  - `.claude/rules/cross-agent-harness.md`
  - `.claude/rules/project-collaboration-profile.md`
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
