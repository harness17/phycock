# Phase 2 統計改善計画

**作成日**: 2026-05-05  
**対象フェーズ**: Phase 2  
**プロジェクト**: Phycock — 体調管理アプリ（ASP.NET Core 10 MVC）

---

## 目的

Phase 1 で登録・閲覧できるようになった体調記録、睡眠記録、通所スケジュールを、本人とスタッフが振り返りやすい形で可視化する。

特に睡眠時間は、単純な合計だけでは「本睡眠が足りないのか」「仮眠が増えているのか」「施設での休息が多いのか」が見えにくいため、`SleepType` ごとに分けて確認できる統計へ拡張する。

---

## スプリントコントラクト（完成条件）

**正常系:**
- [ ] 統計ページの週次睡眠グラフで、本睡眠・仮眠・施設での休息・その他を分けて表示できる
- [ ] 同じ日付に複数の睡眠記録がある場合、同じ `SleepType` ごとに合算される
- [ ] 本睡眠と仮眠を合算した総睡眠時間も確認できる
- [ ] 月次の睡眠統計を確認できる
- [ ] Admin はヘッダー共通の対象ユーザー選択に従って、選択中 Member の統計を確認できる
- [ ] Member は自分の統計だけを確認できる

**認可・前提条件:**
- [ ] 統計 API は `[Authorize]` を維持する
- [ ] 対象ユーザー解決は既存の `ResolveTargetUserIdAsync()` に寄せ、クエリ文字列で任意ユーザー ID を受け取らない
- [ ] 未完了睡眠（`EndDate == null`）や終了が開始以前の不正データは統計から除外する

**異常系:**
- [ ] DB エラー時は既存方針どおり内部情報を露出しないメッセージを返す
- [ ] 日付範囲が不正な場合は空データまたは 400 を返す方針を統一する
- [ ] データが 0 件の日もグラフのラベルは欠落しない

**no-regression:**
- [ ] 既存の週次体調・月次体調グラフは動作を維持する
- [ ] 睡眠記録 CRUD と FullCalendar 表示は動作を維持する
- [ ] `dotnet build Phycock.slnx` と `dotnet test Phycock.slnx` が成功する
- [ ] ブラウザで統計ページの週次・月次切替とグラフ描画を確認する

---

## 現状整理

- `SleepRecordEntity` はすでに `SleepType` を持っている
- `SleepType` は `NightSleep`, `DaytimeNap`, `MedicalFacilityRest`, `Other` の 4 種類
- `StatisticsService.GetWeeklySleepStats()` は現在、睡眠種別を無視して日別合計のみを `SleepHoursData` に詰めている
- `StatisticsViewModels.ChartSeriesDto` は睡眠用データを単一系列としてしか持てない
- 統計ページは Chart.js の棒グラフで睡眠時間を表示している

---

## データ設計判断

Phase 2 の睡眠統計改善では、DB スキーマ変更は行わない。

理由:
- `SleepType` が既に永続化されており、種別別集計に必要な情報は足りている
- 医療・体調系データなので既存記録への破壊的変更を避けたい
- まず表示・集計の改善に集中できる

追加するのは、統計 API 用の DTO とサービス集計ロジックのみとする。

---

## 推奨 UI

### 週次睡眠

積み上げ棒グラフを第一候補にする。

- X 軸: 日付
- Y 軸: 時間
- 系列:
  - 本睡眠
  - 仮眠
  - 施設での休息
  - その他
- 補助表示:
  - 週合計
  - 1日平均
  - 本睡眠平均

積み上げにすると、総睡眠時間と内訳を同時に見られる。ユーザーが見たい「本睡眠・仮眠などを分けて見る」に対して最も直感的。

### 月次睡眠

Phase 2 では月次も睡眠統計を追加する。

候補:
- 日別の積み上げ棒グラフ
- 種別ごとの月合計カード
- 本睡眠の 7 日移動平均線

初回実装は、週次と同じ積み上げ棒グラフ + 月合計カードまでに留める。

---

## 実装ステップ

### Step 1: 睡眠統計 DTO を追加

- [ ] `Phycock/Models/StatisticsViewModels.cs`
  - `SleepTypeSeriesDto` を追加
  - `Labels`
  - `NightSleepHoursData`
  - `DaytimeNapHoursData`
  - `MedicalFacilityRestHoursData`
  - `OtherHoursData`
  - `TotalHoursData`
  - `Summary`

### Step 2: 睡眠統計集計を種別別に変更

- [ ] `Phycock/Service/StatisticsService.cs`
  - `GetWeeklySleepStats()` の戻り値を睡眠専用 DTO に変更
  - `GetMonthlySleepStats(userId, year, month)` を追加
  - `SleepType` ごとに日別合算する共通 private メソッドを追加
  - `EndDate == null` と `EndDate <= StartDate` は除外する

### Step 3: API を追加

- [ ] `Phycock/Controllers/StatisticsController.cs`
  - 既存 `GetSleepWeekly` を新 DTO 返却に更新
  - `GetSleepMonthly(int year, int month)` を追加
  - 例外時のログとレスポンス方針は既存統計 API に合わせる

### Step 4: 統計画面を更新

- [ ] `Phycock/Views/Statistics/Index.cshtml`
  - 週次睡眠グラフを積み上げ棒グラフに変更
  - 月次タブに睡眠グラフを追加
  - 睡眠合計・本睡眠平均・仮眠合計のサマリーを追加
  - モバイル幅でもカード内テキストとグラフが崩れないことを確認する

### Step 5: テスト追加

- [ ] `Tests/Statistics/StatisticsServiceTests.cs`
  - 正常: 同一日に本睡眠と仮眠があると系列別に集計される
  - 正常: 同じ `SleepType` が同日に複数あると合算される
  - 境界: 記録 0 件でも 7 日分または月日数分のラベルが返る
  - 異常: `EndDate == null` は除外される
  - 異常: `EndDate <= StartDate` は除外される
  - 月次: 月末日数に応じたラベルとデータ数になる

---

## Phase 2 後半候補

初回の睡眠統計が安定した後に検討する。

- 体調スコアと睡眠時間の相関表示
- 通所予定・出席状況と睡眠傾向の並列表示
- Excel / PDF エクスポート
- 期間指定の自由化（過去 7 日、過去 30 日、任意期間）
- 体調悪化前の睡眠不足傾向を見つける支援者向けビュー

---

## 保留事項

- 本睡眠と仮眠を足した「総睡眠時間」を健康指標として強調しすぎると、長時間仮眠が良いように見える可能性がある。主要指標は「本睡眠」、補助指標として「総睡眠時間」を扱う。
- `MedicalFacilityRest` を睡眠時間に含めるかは運用次第。初期表示では別系列にして、総睡眠時間には含めるが、本睡眠平均には含めない。
- 未完了睡眠を統計に出す場合は、別途「記録中」や「未完了」扱いが必要。Phase 2 初回では除外する。
