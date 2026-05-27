# Phycock 共同開発ハンドオフ

最終更新: 2026-05-22
対象リポジトリ: `H:/ClaudeCode/Phycock`
status: active

このファイルは Codex と Claude Code の相互ハンドオフ log。書式・更新タイミングは `.claude/rules/handoff-protocol.md`、汎用ハーネスは `.claude/rules/cross-agent-harness.md`、Phycock 固有 profile は `.claude/rules/project-collaboration-profile.md` を参照。

新しいタスクは下記「進行中」にこのファイル最上部へ追記する（最新が上）。完了・マージ済みセクションは `docs/handoffs/archive/YYYY-QN.md` へ切り出す。

---

## 進行中

### 2026-05-27 セキュリティレビュー指摘修正：PDF出力のHost依存とCookie転送範囲（Claude Code → Codex 依頼）

- 対象: `Phycock/Controllers/StatisticsController.cs`（`ExportPdf` メソッドのみ）
- 作成者: Claude Code
- 主題: `/security-review` で挙がった注意 2 点を修正する。
  1. `ExportPdf` が `Request.Host` から内部URLを組み立て、認証クッキーの `Domain` も `Request.Host.Host` から取っているため、`AllowedHosts="*"` と組み合わさると Host ヘッダーインジェクションで Playwright が外部ホストへ navigate し得る。ループバック固定にする。
  2. `Request.Cookies` を全件 Playwright に転送している。Antiforgery 等の不要 Cookie まで渡るため、認証に必要なものだけに絞る。

- 触ってよい範囲:
  - `Phycock/Controllers/StatisticsController.cs` の `ExportPdf` メソッド内のみ
- 触ってはいけない範囲:
  - `Service/PdfExportService.cs`（呼び出し契約は変えない。`url` と `IEnumerable<PWCookie>` を渡す形は維持）
  - `appsettings.json` の `AllowedHosts`（運用設定なので別タスク）
  - PeriodReflection 関連の新規ファイル、`Index` / `GetHealthWeekly` / `GetSleepWeekly` などの他アクション

- 完成条件:
  1. **ループバック固定**: `ExportPdf` 内で Playwright に渡す `url` を `http://127.0.0.1:{Request.Host.Port ?? 80}{pathBase}/Statistics?print=1&...` 形式に変更する。`Request.Host.Host`（ドメイン名）には依存させない。`Request.Scheme` も `http` 固定（ループバック）にする。
     - `Request.Host.Port` が null（IIS 等で host header にポートが無い場合）には `Request.Host.Value` のポート部を補完するか、HttpContext の `Connection.LocalPort` をフォールバックとして使う。実装はどちらでもよいが理由をコメントで明記する。
  2. **Cookie 絞り込み**: Playwright へ転送する Cookie を「認証に必要なものだけ」に絞る。具体的には以下のプレフィックスで始まる Cookie のみ転送する：
     - `.AspNetCore.Identity.Application`（Identity 認証 Cookie）
     - `.AspNetCore.Session`（Admin の選択中Member ID を保持しているため必要）
     - Antiforgery Cookie（`.AspNetCore.Antiforgery.*`）は **転送しない**。
  3. **Cookie の Domain**: ループバック固定に伴い、`PWCookie.Domain` は `"127.0.0.1"` 固定にする。`Secure=false`、`HttpOnly`/`SameSite` は現状維持。
  4. PDF が従来通り週次 / 月次の両方で正しく生成され、ダウンロードできる（admin が選択中Member 切替後の PDF も含む）。
  5. 既存テスト `dotnet test Phycock.slnx` 全通過。

- verify コマンド:
  ```
  dotnet build Phycock.slnx
  dotnet test Phycock.slnx
  ```
  さらに `dotnet run --project Phycock` で起動して、admin / member それぞれで以下を実機確認：
  - 週次レポートの PDF 出力が動く
  - 月次レポートの PDF 出力が動く
  - admin で対象Member切替後の PDF が選択Member分になる

- レビュー観点（Claude Code が後でレビュー）:
  - URL組み立てが `127.0.0.1` 固定かつ `Request.Host.Host` を参照していないか
  - Cookie 転送が allowlist（プレフィックス一致）方式で、Antiforgery が含まれていないか
  - Playwright 側 `PWCookie.Domain` が `"127.0.0.1"` になっているか
  - エラー処理・ログ出力の変更不要部分が温存されているか
  - `PdfExportService` 側の引数契約が変わっていないか

- 既知リスク:
  - IIS in-process 配置で `Connection.LocalPort` が期待通りのポートを返すかは要確認。確認できない場合は handoff の「未解決」に書き残してよい。
  - ループバック固定により、リバースプロキシ配下で `Statistics?print=1` が直接アプリ側 Kestrel/IIS ポートにバインドされていない構成だと到達できなくなる。本プロジェクトはこの構成を取らないので問題ないはずだが、不安があれば Open Questions に書く。

- **作業フロー（必ずこの順序で行う）**:
  1. **Codex 側セキュリティレビュー**: 上記の Claude Code 指摘 2 点に加え、Codex 独自の視点で `ExportPdf` 周辺（および呼び出し先 `PdfExportService`）に対してセキュリティレビューを実施する。重大/注意の区分で handoff に追記する。`/security-review` 相当の観点（シークレット・認証/認可/IDOR・入力検証・XSS・SQLi・エラー漏洩・ファイル名/パス）を網羅すること。
  2. **両レビュー指摘の統合**: Claude Code 指摘 2 点と、Codex 自身の追加指摘をマージし、修正対象リストを handoff に確定させる。判断に迷う指摘は「保留」として残してよい（その場合は理由を明記）。
  3. **実装**: 統合リストに基づきまとめて修正する（コミットは 1 つにまとめて可）。
  4. **セルフ verify**: 上記 verify コマンド実行 → 結果を handoff に追記。
  5. Claude Code が最終レビュー & merge 判断。

- 次アクション:
  - Codex が「Codex 側セキュリティレビュー結果」を本セクションに追記 → 統合修正リスト確定 → 実装 → verify → 報告。

#### 2026-05-27 追記（Codex 側セキュリティレビュー結果 — Codex 作成）

- レビュー対象:
  - `Phycock/Controllers/StatisticsController.cs` の `ExportPdf`
  - `Phycock/Service/PdfExportService.cs` の `RenderPdfAsync`
  - 参照確認: `Program.cs` の Cookie/Session 設定、`UserManagementService.GetSelectedMemberUserIdAsync`
- 重大:
  - なし。`ExportPdf` は `[Authorize]` 配下で、対象ユーザー解決は既存の Admin 選択Member / Member本人の境界に乗っている。`weekStart` と `section` から任意ユーザーID・任意パス・任意ファイルパスは作れない。
- 注意:
  1. `ExportPdf` の内部レンダリングURLが `Request.Scheme` / `Request.Host` 依存。`AllowedHosts="*"` と組み合わさるため、Host ヘッダー注入で Playwright の `GotoAsync` が外部ホストへ向く余地がある。Claude Code 指摘通り修正対象。
  2. `Request.Cookies` 全件を Playwright に転送している。Antiforgery など PDF表示に不要な Cookie まで別ブラウザコンテキストへ渡るため、最小権限の観点で Identity 認証 Cookie と Session Cookie に限定する。Claude Code 指摘通り修正対象。
- 確認済み観点:
  - シークレット: 対象コードに API key / connection string / token のハードコードなし。
  - 認証・認可・IDOR: PDF出力は `Statistics/Index` と同じ `ResolveTargetUserIdAsync` 経由。Admin の選択Memberは `GetMemberListAsync` で Member ロールかつ有効ユーザーに限定される。
  - 入力検証: `section` は `monthly` 以外を `weekly` 扱いに正規化済み。`weekStart` は `NormalizeWeekStart` で日付化され、URLには `yyyy-MM-dd` のみ入る。
  - XSS: Razor 表示は通常の `@` エンコードが主で、JSON埋め込みは `Json.Serialize` 経由。今回修正対象外の追加 XSS は見つからず。
  - SQLi: 対象メソッド内で SQL 文字列連結なし。
  - エラー漏洩: 利用者向けレスポンスは固定文言。ログには `weekStart` / `section` / 内部URL程度で、体調本文やCookie値は出ていない。
  - ファイル名/パス: ダウンロードファイル名のユーザー名部分は `SanitizeFileNamePart` でファイル名禁止文字と空白を除去。サーバー側ファイルパス書き込みはなし。
- 統合修正リスト:
  1. `ExportPdf` の Playwright URL を `http://127.0.0.1:{port}{pathBase}/Statistics?print=1&weekStart=...&section=...` に固定する。`Request.Host.Host` と `Request.Scheme` は使用しない。
  2. ポートは `Request.Host.Port` を優先し、null の場合はローカル待受ポートとして `HttpContext.Connection.LocalPort` にフォールバックする。理由コメントをコードに残す。
  3. Playwright へ渡す Cookie は `.AspNetCore.Identity.Application` と `.AspNetCore.Session` のプレフィックス一致に限定し、`Domain = "127.0.0.1"` に固定する。
  4. `PdfExportService.RenderPdfAsync` の契約は変更しない。
- 保留:
  - IIS in-process / リバースプロキシ配下で `Connection.LocalPort` によるループバック到達が常に成立するかは環境依存。実機確認できない場合は未解決として残す。

#### 2026-05-27 追記（Claude Code クロスレビュー結果）

- 差分対象: `Phycock/Controllers/StatisticsController.cs` の `ExportPdf` のみ。`PdfExportService` 等の禁止範囲は無変更を確認。
- 重大指摘: なし。
- 完成条件チェック:
  1. ループバック固定 ✅ `http://127.0.0.1:{port}{pathBase}/...`、`Request.Host.Host` / `Request.Scheme` 参照なし。
  2. ポートフォールバック ✅ `Request.Host.Port` → `HttpContext.Connection.LocalPort` → `80`。理由コメント有り。
  3. Cookie 絞り込み ✅ `.AspNetCore.Identity.Application` / `.AspNetCore.Session` プレフィックス一致のみ、`StringComparison.Ordinal`。Antiforgery 含まれず。
  4. `PWCookie.Domain = "127.0.0.1"` 固定 ✅。
  5. `PdfExportService` 契約変更なし ✅。
- 軽微な観察（修正不要）:
  - Identity が将来 External login / TwoFactorUserId 等の別 Cookie を発行するシナリオでは追加 allowlist が必要になり得るが、現フローでは不要。
- 未確認:
  - 実動確認（admin / member、週次 / 月次 PDF、admin 選択Member切替後の対象）はユーザー側でブラウザ確認が必要。Codex 側でも未実施。
  - IIS in-process / リバースプロキシ配下での `Connection.LocalPort` 到達性は環境依存（既知リスク継続）。
- 判定: merge ブロッカーなし。実動確認 OK ならコミット可。

#### 2026-05-27 12:41 追記（実装・セルフ verify — Codex 作成）

- 変更内容:
  - `ExportPdf` の Playwright 内部URLを `http://127.0.0.1:{port}{pathBase}/Statistics?...` に固定し、`Request.Host.Host` と `Request.Scheme` 由来の外部ホスト遷移余地を除去。
  - ポートは `Request.Host.Port` 優先、未指定時は `HttpContext.Connection.LocalPort`、0以下なら 80 にフォールバック。
  - Playwright に渡す Cookie を `.AspNetCore.Identity.Application` / `.AspNetCore.Session` のプレフィックス一致に限定し、`PWCookie.Domain` を `127.0.0.1` 固定に変更。Antiforgery Cookie は転送対象外。
  - `PdfExportService.RenderPdfAsync` の呼び出し契約は変更なし。
- セルフ verify:
  - `dotnet build Phycock.slnx` 成功（0 warnings / 0 errors）。
  - `dotnet test Phycock.slnx` 成功（106 passed / 0 failed）。
  - 初回のサンドボックス内 `dotnet build Phycock.slnx` は `C:\Users\harne\AppData\Roaming\NuGet\NuGet.Config` 読み取り権限で失敗。承認付き再実行で成功。
  - `git diff --check` 成功（空白エラーなし）。
- 実動確認:
  - 未実施。admin / member での週次・月次 PDF ダウンロード、および admin 選択Member切替後の PDF 対象確認は Claude Code 側レビューで実施が必要。
- 未解決:
  - IIS in-process / リバースプロキシ配下で `Connection.LocalPort` のループバック到達性は未実機確認。
- 次アクション:
  - Claude Code が `ExportPdf` 差分をレビューし、実ブラウザで週次・月次・admin選択Memberの PDF 出力を確認する。


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
