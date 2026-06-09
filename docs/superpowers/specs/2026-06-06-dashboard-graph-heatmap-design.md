# 設計仕様: ダッシュボード強化・トレンドグラフ・体調ヒートマップ（Phase 1）

**作成日**: 2026-06-06  
**ステータス**: 改訂済み（Codex レビュー反映）  
**スコープ**: DBスキーマ変更なし。既存 Controller / Service / View を拡張する。

---

## 概要

Phycock の可視化・ルーティン支援を強化する Phase 1。以下の3コンポーネントを変更する。

1. **デイリーチェックリスト** — 既存 DashboardService / DashboardViewModel を拡張し、記録状況チェックリストを追加
2. **トレンドグラフ（期間移動）** — 既存統計グラフに前週/次週の期間移動 UI を追加（既存グラフを置換しない）
3. **体調ヒートマップ** — カレンダーの各日付セルを体調レベルで色付け（`dayCellDidMount` 方式）

---

## 全体アーキテクチャ

```
拡張するもの
├── DashboardService / DashboardViewModel     ← チェックリスト情報を追加
├── HomeController / Home/Index.cshtml         ← チェックリスト UI を追加
├── StatisticsController                       ← 期間移動 JSON API を追加
├── Statistics/Index.cshtml                   ← 期間移動 UI（前週/次週ボタン）
├── HealthRecordController or Service          ← ヒートマップ用日別集計 API を追加
└── wwwroot/js/site.js (calendar 部分)        ← dayCellDidMount でセル着色

新規追加するもの
└── なし（既存 Service / ViewModel / CDN をすべて流用）

触らないもの
└── Entity / Migration / DB スキーマ
```

**データフロー：**
- チェックリスト: 既存 `DashboardService.GetDashboard()` の返却 ViewModel に記録状況フラグを追加 → View に渡す
- 期間移動: `StatisticsController` に `GET /Statistics/TrendData?weekStart=YYYY-MM-DD` を追加。既存 `StatisticsService.GetWeeklyReport` を流用 → 既存 Chart.js が JSON を受けて再描画
- ヒートマップ: `HealthRecordController` に `GET /HealthRecord/HeatmapData?start=...&end=...` を追加。日別の最低体調レベルを返す → `dayCellDidMount` でセル背景色を適用

---

## コンポーネント詳細

### 1. デイリーチェックリスト（既存ダッシュボードの拡張）

**現行:** `DashboardService` は `TodayHealthRecords` / `TodayScheduleEntries` / `WeeklySummary` を返している。

**追加する表示内容：**

```
┌─────────────────────────────────────────┐
│ 今日の記録  2026-06-06 (土)             │
├──────────────┬──────────────────────────┤
│ ✅ 体調記録  │ 2件 登録済み → [一覧]   │
│ ⬜ 睡眠記録  │ 未記録 → [登録する]      │
│ 🔘 通所予定  │ 休日（予定なし）         │
├──────────────┴──────────────────────────┤
│ 直近の体調: 体調「普通」気分「やや良い」 │
└─────────────────────────────────────────┘
```

**実装方針：**
- **新規 Service / ViewModel は作らない。** 既存 `DashboardViewModel` にチェックリスト用プロパティを追加する
  - `bool HasSleepRecord` — 今日の睡眠記録があるか
  - `int HealthRecordCount` — 今日の体調記録件数
  - `ConditionLevel? LatestCondition` — 直近の体調レベル
  - `FeelingLevel? LatestFeeling` — 直近の気分レベル
- 既存 `DashboardService.GetDashboard()` に睡眠記録チェックと最新体調取得のロジックを追加
- 既存の `TodayHealthRecords` / `TodayScheduleEntries` はそのまま維持し、チェックリストの表示根拠としても使う

**チェックリスト完了条件の定義：**

| 項目 | 完了条件 | 表示 |
|------|---------|------|
| 体調記録 | `TodayHealthRecords.Count >= 1`（1件以上あれば完了） | ✅ N件 登録済み |
| 睡眠記録 | `HasSleepRecord == true`（RecordDate = 今日 の記録が1件以上、または EndDate が今日の記録が1件以上） | ✅ 登録済み |
| 通所予定 | `TodayScheduleEntries.Count >= 1` → 予定あり、`== 0` → 休日 | ✅ 予定あり / 🔘 休日 |

- 通所予定なし（休日）は未記録ではなく正常な状態として表示する（⬜ ではなく 🔘）
- 未記録の項目には登録ページへの直リンクを表示

**エラー時の表示：**
- `DashboardService` が例外を投げた場合、「記録状況を取得できませんでした」と表示する（既存の共通エラー方針に合わせる）
- **「未記録」として表示しない**（実データがあるのに登録を促し重複操作を誘発するため）

---

### 2. トレンドグラフ期間移動（Statistics ページ強化）

**現行:** 統計ページには Chart.js 4.4.7 CDN 導入済み。`GetHealthWeekly` / `GetWeeklySleepStats` で体調折れ線 + 睡眠棒グラフが既に存在する。週の起点は日曜始まり。

**追加するもの：** 既存グラフを置換せず、**前週/次週の期間移動 UI** を追加する。

```
[← 前週] 2026-06-01 〜 2026-06-07 [次週→]
```

**実装方針：**
- `StatisticsController` に期間移動用の JSON API を追加:
  - エンドポイント: `GET /Statistics/TrendData?weekStart=YYYY-MM-DD`
  - `weekStart` は `NormalizeWeekStart()` で日曜始まりに正規化する（既存ロジックと同一）
  - 認可: `ResolveTargetUserIdAsync()` を必ず使用し、Admin は選択中 Member、Member は本人のみ
  - レスポンス: 既存 `GetWeeklyReport` と同じ DTO を返す（新しいレスポンス型は作らない）
  - `weekStart` の範囲制限: 今日から ±365 日以内。範囲外は `BadRequest`
  - 未来の週は許可する（記録がなければ空データ）
- フロント側は既存の `reloadStats` ボタンのロジックを拡張し、前週/次週ボタンで `weekStart` パラメータを変えて fetch → 既存 Chart.js を再描画
- **週始まり**: 既存統計と同じ日曜始まり（仕様のラベル例「月火水…」は図の便宜上のもの。実装は日→土）

**TrendData と既存 API の関係：**
- `TrendData` は `GetHealthWeekly` + `GetWeeklySleepStats` を1回のリクエストにまとめた便利 API
- 月次の期間移動は既存の月次タブと `weekStart` パラメータで対応済み（追加不要）

**未記録と 0 の区別：**
- 睡眠時間: 未記録の日は `null` を返す。睡眠 0 時間（実際には起こらない）は `0` を返す
- 既存 `GetWeeklySleepStats` が `0` を返す箇所は、`null` に変更する（破壊的変更だが後方互換の影響は Statistics ページ内のみ）
- グラフ上では `null` の日は線を途切れさせる（Chart.js の `spanGaps: false`）

---

### 3. 体調ヒートマップ（Calendar 強化）

**現行:** `CalendarController` は `Index()` のみ。イベント JSON は `HealthRecordController.GetEvents` → `HealthRecordService.GetEventsForCalendar` から返しており、イベントごとに `backgroundColor` / `borderColor` はすでに設定済み。

**追加するもの：** イベントの色ではなく、**日付セル全体の背景色** を体調レベルで着色する。

```
┌────┬────┬────┬────┬────┬────┬────┐
│ 月 │ 火 │ 水 │ 木 │ 金 │ 土 │ 日 │
├────┼────┼────┼────┼────┼────┼────┤
│ 2  │ 3  │ 4  │ 5  │ 6  │ 7  │ 8  │
│[赤]│[橙]│[黄]│[緑]│[緑]│    │    │  ← セル背景
│ ── │ ── │ ── │ ── │ ── │    │    │  ← イベントは通常通り
└────┴────┴────┴────┴────┴────┴────┘

凡例: 赤=悪い 橙=やや悪い 黄=普通 黄緑=やや良い 緑=良い
```

**体調レベルと色のマッピング（半透明）：**

| ConditionLevel | セル背景色（半透明） |
|---|---|
| VeryBad (1) | `rgba(239, 83, 80, 0.2)` |
| Bad (2) | `rgba(255, 152, 0, 0.2)` |
| Normal (3) | `rgba(255, 238, 88, 0.2)` |
| Good (4) | `rgba(156, 204, 101, 0.2)` |
| VeryGood (5) | `rgba(102, 187, 106, 0.2)` |

**半透明にする理由:** 既存のイベント文字・日付文字・選択状態のコントラストを維持するため。

**実装方針：**
- **イベント JSON には手を加えない。** 代わりに以下を行う:
  1. `HealthRecordController`（または `HealthRecordService`）に日別集計 API を追加:
     - エンドポイント: `GET /HealthRecord/HeatmapData?start=YYYY-MM-DD&end=YYYY-MM-DD`
     - 認可: Admin は選択中 Member、Member は本人のみ（既存 `GetEvents` と同じパターン）
     - レスポンス: `[{ "date": "2026-06-02", "level": 1 }, ...]`
     - その日の最低 ConditionLevel を代表値とする（体調の悪さを見逃さないため）
     - 記録なしの日はレスポンスに含めない
  2. `wwwroot/js/site.js` の FullCalendar 初期化に `dayCellDidMount` コールバックを追加:
     - カレンダー初期化時と `datesSet`（月移動時）に HeatmapData を fetch
     - `dayCellDidMount` でセルの `background-color` を半透明色で設定
  3. 体調表示トグルOFF時はヒートマップも非表示にする（トグル状態と連動）

- **取得範囲の制限:**
  - `start` と `end` の差は最大 42 日（FullCalendar の月表示 = 最大 6 週）
  - 範囲超過は `BadRequest`
  - 既存 `HealthRecordRepository.GetByUserAndRange` はユーザーID・日付範囲で絞り込むため、パフォーマンスは問題なし

---

## エラー処理・境界値

| 状況 | 対処 |
|------|------|
| 体調記録 0 件の日 | ヒートマップ色なし、グラフは `null` で線を途切れさせる |
| 睡眠記録なしの日 | グラフは `null`（`0` と区別する） |
| TrendData の `weekStart` 範囲外 | `BadRequest` を返す（今日 ±365 日） |
| HeatmapData の `start-end` が 42 日超 | `BadRequest` |
| DashboardService 例外 | 「記録状況を取得できませんでした」と表示（「未記録」扱いにしない） |
| 通所予定なし | 休日として正常表示（未記録扱いにしない） |

---

## テスト観点

| テスト対象 | テストケース |
|---|---|
| DashboardService | 今日の記録あり/なし/一部あり のパターンでフラグが正しく設定される |
| DashboardService | 前夜跨ぎの睡眠記録（StartDate=昨夜、EndDate=今朝）が HasSleepRecord=true になる |
| DashboardService | 通所予定なし（休日）の場合にエラーにならない |
| DashboardService | 複数予定（AM/PM）がある場合に全件表示される |
| DashboardService | 例外発生時に unavailable 状態を返す（未記録扱いにしない） |
| StatisticsController TrendData | `ResolveTargetUserIdAsync()` で Member 本人のみ返す |
| StatisticsController TrendData | Admin が選択中 Member のデータのみ返す |
| StatisticsController TrendData | 未認証でアクセス → 401 |
| StatisticsController TrendData | `weekStart` が日曜に正規化される |
| StatisticsController TrendData | `weekStart` 範囲外（±365日超）→ 400 |
| StatisticsController TrendData | 不正な `weekStart`（parse 不可）→ 400 |
| StatisticsController TrendData | 記録なし週 → 空データ |
| HealthRecordController HeatmapData | 複数記録の日に最低レベルが選ばれる |
| HealthRecordController HeatmapData | 記録なし日はレスポンスに含まれない |
| HealthRecordController HeatmapData | `start-end` 42日超 → 400 |
| HealthRecordController HeatmapData | Admin/Member の対象ユーザー分離 |
| 既存ダッシュボード | 既存表示（TodayHealthRecords / TodayScheduleEntries / WeeklySummary）が回帰しない |
| 既存統計グラフ | 既存 `GetHealthWeekly` / `GetWeeklySleepStats` のレスポンスと描画が回帰しない |

---

## 変更ファイル一覧（実装時の参照用）

| ファイル | 変更種別 |
|---|---|
| `Phycock/Service/DashboardService.cs` | 変更（チェックリスト情報追加） |
| `Phycock/Models/DashboardViewModel.cs` | 変更（プロパティ追加） |
| `Phycock/Controllers/HomeController.cs` | 微修正（エラーハンドリング） |
| `Phycock/Views/Home/Index.cshtml` | 変更（チェックリスト UI 追加） |
| `Phycock/Controllers/StatisticsController.cs` | 変更（TrendData API 追加） |
| `Phycock/Views/Statistics/Index.cshtml` | 変更（前週/次週ボタン追加） |
| `Phycock/Controllers/HealthRecordController.cs` | 変更（HeatmapData API 追加） |
| `Phycock/Service/HealthRecordService.cs` | 変更（日別集計メソッド追加） |
| `Phycock/wwwroot/js/site.js` | 変更（dayCellDidMount 追加） |

---

## 対象外（Phase 2 以降）

- リマインダー通知（BackgroundService + Push / メール）
- 相関分析・症状頻度ランキング
- バイタル（体温・血圧・体重）記録
- 月次の期間移動（既存タブで対応済み）
