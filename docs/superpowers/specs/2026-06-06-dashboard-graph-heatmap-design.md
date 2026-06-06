# 設計仕様: ダッシュボード強化・トレンドグラフ・体調ヒートマップ（Phase 1）

**作成日**: 2026-06-06  
**ステータス**: 承認済み  
**スコープ**: DBスキーマ変更なし。既存Controller/Service/Entityを拡張する。

---

## 概要

Phycock の可視化・ルーティン支援を強化する Phase 1。以下の3コンポーネントを追加・変更する。

1. **デイリーチェックリスト** — Home/Index をダッシュボード化し、今日の記録状況を一覧表示
2. **トレンドグラフ** — 統計ページに Chart.js の折れ線・棒グラフを追加
3. **体調ヒートマップ** — カレンダーの各日付セルを体調レベルで色付け

---

## 全体アーキテクチャ

```
変更するもの
├── HomeController / Home/Index.cshtml         ← デイリーダッシュボード化
├── StatisticsController                       ← グラフ用 JSON API を追加
├── Statistics/Index.cshtml                   ← Chart.js トレンドグラフ追加
├── CalendarController                        ← 体調色情報をレスポンスに追加
└── Calendar/Index.cshtml / calendar.js       ← ヒートマップ色表示

新規追加するもの
├── HomeService（または既存 Service を拡張）   ← 今日の記録状況を集約
├── DailyDashboardViewModel                   ← ダッシュボード用 ViewModel
└── Chart.js（CDN 追加）                      ← グラフ描画ライブラリ

触らないもの
└── Entity / Migration / DB スキーマ
```

**データフロー：**
- ダッシュボード: `HomeService` が HealthRecord / SleepRecord / ScheduleEntry を日付で集計 → View に渡す
- トレンドグラフ: `StatisticsController` の既存集計ロジックを JSON API として公開 → フロントの Chart.js が描画
- ヒートマップ: `CalendarController` のイベント JSON に `backgroundColor` フィールドを追加 → FullCalendar が色付きで表示

---

## コンポーネント詳細

### 1. デイリーチェックリスト（Home/Index）

**表示内容：**

```
┌─────────────────────────────────────────┐
│ 今日の記録  2026-06-06 (土)             │
├──────────────┬──────────────────────────┤
│ ✅ 体調記録  │ 起床時・就眠時 登録済み  │
│ ⬜ 睡眠記録  │ 未記録 → [登録する]      │
│ ✅ 通所予定  │ 午前 在宅               │
├──────────────┴──────────────────────────┤
│ 直近の体調: 体調「普通」気分「やや良い」 │
└─────────────────────────────────────────┘
```

**実装方針：**
- `HomeService` を新規作成し、今日の日付で HealthRecord / SleepRecord / ScheduleEntry を各 Repository から取得して集約する
- `DailyDashboardViewModel` を作成（記録済みフラグ・サマリー情報を保持）
  - `bool HasHealthRecord`
  - `bool HasSleepRecord`
  - `bool HasScheduleEntry`
  - `IList<string> HealthRecordTimings`（記録済みのタイミング名）
  - `ConditionLevel? LatestCondition`
  - `FeelingLevel? LatestFeeling`
  - `ScheduleEntryEntity? TodaySchedule`
- 未記録の項目には登録ページへの直リンクを表示
- 既存の Home/Index を置き換え（実装前に現状の表示内容を確認すること）

---

### 2. トレンドグラフ（Statistics ページ強化）

**グラフ構成：**

```
体調トレンド  [週次▼]  [← 前週] [今週] [次週→]
─────────────────────────────────────────────────
5 ┤          ●━━━●
4 ┤    ●━━━━         ●━━━●                     ← 体調（折れ線）
3 ┤                        ●━━━●━━━●
2 ┤                                              ← 気分（折れ線）
1 ┤
  └──┬──┬──┬──┬──┬──┬──
     月  火  水  木  金  土  日

睡眠時間（h）
8h ┤   ██   ██   ██
6h ┤██    ██    ██   ██  ██                     ← 睡眠時間（棒グラフ）
```

**実装方針：**
- `Chart.js`（CDN）を Statistics/_Layout または Statistics/Index に追加
- `StatisticsController` に以下の JSON API を追加:
  - エンドポイント: `GET /Statistics/TrendData?range=week&offset=0`
  - `range`: `week` または `month`
  - `offset`: 0=今週/今月、-1=前週/前月、+1=次週/次月
  - レスポンス形式:
    ```json
    {
      "labels": ["月", "火", "水", "木", "金", "土", "日"],
      "condition": [3, 4, 3, null, 4, null, null],
      "feeling": [4, 4, 3, null, 4, null, null],
      "sleepHours": [7.5, 6.0, 8.0, null, 6.5, null, null]
    }
    ```
  - 記録のない日は `null`（グラフ上で線が途切れる）
- 既存の週次/月次切り替えUIと統合（タブまたはセレクト）
- 体調・気分は折れ線グラフ（左Y軸、1〜5）、睡眠時間は棒グラフ（右Y軸、0〜12h）で重ねる
- 既存集計ロジック（`StatisticsService`）を流用して二重実装を避ける

---

### 3. 体調ヒートマップ（Calendar 強化）

**表示イメージ：**

```
┌────┬────┬────┬────┬────┬────┬────┐
│ 月 │ 火 │ 水 │ 木 │ 金 │ 土 │ 日 │
├────┼────┼────┼────┼────┼────┼────┤
│ 2  │ 3  │ 4  │ 5  │ 6  │ 7  │ 8  │
│[赤]│[橙]│[黄]│[緑]│[緑]│    │    │
└────┴────┴────┴────┴────┴────┴────┘

凡例: 赤=悪い 橙=やや悪い 黄=普通 黄緑=やや良い 緑=良い
```

**体調レベルと色のマッピング：**

| ConditionLevel | 色コード |
|---|---|
| VeryBad (1) | `#ef5350`（赤） |
| Bad (2) | `#ff9800`（橙） |
| Normal (3) | `#ffee58`（黄） |
| Good (4) | `#9ccc65`（黄緑） |
| VeryGood (5) | `#66bb6a`（緑） |

**実装方針：**
- `CalendarController` の既存イベント JSON レスポンスに `backgroundColor` / `borderColor` フィールドを追加
- HealthRecord を日付単位で集計し、その日の**最低レベル**（体調の悪さを見逃さないため）を代表値とする
- 記録が複数ある日（朝・夜など）: `Math.Min()` で最低 `ConditionLevel` を選択
- 体調記録がない日は `backgroundColor` を設定しない（現状と変わらず）
- FullCalendar は既存のまま利用（イベント側に色情報があれば自動で背景色を表示）

---

## エラー処理・境界値

- 体調記録が0件の日はヒートマップ色なし、グラフは `null` で線を途切れさせる
- 統計 API の `offset` 範囲は `-24〜+24`（約2年分）を上限とする（不正値は BadRequest）
- `HomeService` が例外を投げた場合はデフォルト「未記録」状態として表示（ダッシュボードがクラッシュしない）

---

## テスト観点

| テスト対象 | テストケース |
|---|---|
| HomeService | 今日の記録あり/なし/一部あり の各パターンでフラグが正しく設定される |
| StatisticsController TrendData | week/month 切り替え、offset 範囲外、記録なし週 |
| CalendarController | 複数記録の日に最低レベルが選ばれる、記録なし日は色なし |
| ViewModel | LatestCondition が複数記録から最新を返す |

---

## 変更ファイル一覧（実装時の参照用）

| ファイル | 変更種別 |
|---|---|
| `Phycock/Controllers/HomeController.cs` | 変更（HomeService 呼び出し追加） |
| `Phycock/Controllers/StatisticsController.cs` | 変更（TrendData API 追加） |
| `Phycock/Controllers/CalendarController.cs` | 変更（backgroundColor 追加） |
| `Phycock/Service/HomeService.cs` | 新規 |
| `Phycock/Models/DailyDashboardViewModel.cs` | 新規 |
| `Phycock/Views/Home/Index.cshtml` | 変更（ダッシュボード UI） |
| `Phycock/Views/Statistics/Index.cshtml` | 変更（Chart.js グラフ追加） |
| `Phycock/Views/Calendar/Index.cshtml` または `wwwroot/js/calendar.js` | 変更（色表示） |

---

## 対象外（Phase 2 以降）

- リマインダー通知（BackgroundService + Push / メール）
- 相関分析・症状頻度ランキング
- バイタル（体温・血圧・体重）記録
