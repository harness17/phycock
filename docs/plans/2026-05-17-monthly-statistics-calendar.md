# 月次統計カレンダー 実装計画

作成日: 2026-05-17
対象ブランチ: codex-schedule-activity-types（または develop）

## 概要

統計ページ（`Views/Statistics/Index.cshtml`）の「月次」タブに、月グリッド形式のカレンダーを追加する。
各日セルに 体調平均・気分平均・睡眠時間合計・通所 を色分け表示する。
表示する月は既存ツールバーの `weekStart` が属する月（既存の月次チャートと同じ規約）。

## スプリントコントラクト（完成条件）

- 正常系: 月次タブを開くと当月のカレンダーグリッド（日曜開始の週行）が表示される
- 正常系: 記録のある日は体調・気分・睡眠・通所の色が付く
- 正常系: 睡眠時間が 5h/6.5h/8h/10h の日でそれぞれ 赤・黄・緑・青 になる
- 利用前提: Admin は選択中 Member、Member は自分のデータのみ表示（既存 `ResolveTargetUserIdAsync` を流用）
- 異常系: 記録ゼロの月でもグリッドが崩れず、記録なしは灰表示
- 非回帰: 週次レポート・既存の月次チャート・PDF出力が引き続き動作する
- `dotnet build Phycock.slnx` / `dotnet test Phycock.slnx` が pass する

## 睡眠時間の色分け基準（週次・月次共通の定数）

4段階。週次・月次で共通の定数として一元管理する。

| 区分 | 範囲 | 色 |
|------|------|-----|
| 不足 (Insufficient) | < 6h | 赤 |
| やや不足 (SlightlyShort) | 6h 以上 7h 未満 | 黄 |
| 適正 (Adequate) | 7h 以上 9h 以下 | 緑 |
| 過剰 (Excessive) | > 9h | 青 |

既存の週次チャートの睡眠目安帯（現在 6–8h）も **7–9h** に統一更新する。

## 実装タスク（チェックボックス順）

### 1. 共通定数の新規作成

- [ ] `Phycock/Common/SleepStandards.cs` を新規作成する
  - 4段階を表す enum `SleepLevel { None, Insufficient, SlightlyShort, Adequate, Excessive }`
  - 適正帯の下限・上限定数 `AdequateLowerHours = 7`, `AdequateUpperHours = 9`
  - メソッド `SleepLevel Classify(double totalHours)`:
    - `totalHours <= 0` → `None`
    - `< 6` → `Insufficient`
    - `< 7` → `SlightlyShort`
    - `<= 9` → `Adequate`
    - それ以外 → `Excessive`

### 2. DTO の追加

- [ ] `Phycock/Models/StatisticsViewModels.cs` に以下を追加する
  - `MonthlyCalendarDto`:
    - `int Year`
    - `int Month`
    - `List<MonthlyDayCellDto> Cells`（日曜開始・末尾は土曜まで埋めた完全な週グリッド）
  - `MonthlyDayCellDto`:
    - `DateTime Date`
    - `bool InMonth`（当月の日か。前後月の埋めセルは false）
    - `double? ConditionAvg`
    - `double? FeelingAvg`
    - `double SleepTotalHours`（本睡眠＋他睡眠の当日重なり時間合計）
    - `SleepLevel SleepLevel`
    - `string ScheduleDayClass`（既存と同じ planned/remote/rest）
    - `string ScheduleSummary`（通所の短いサマリ。例「AM 通所予定」「予定なし」。複数あれば「、」連結）

### 3. Service への集計メソッド追加

- [ ] `Phycock/Service/StatisticsService.cs` に `GetMonthlyCalendar(string userId, int year, int month)` を追加する
  - 当月 1日〜末日のデータを `HealthRecordRepository.GetByUserAndRange` /
    `SleepRecordRepository.GetByUserAndRange` /
    `ScheduleEntryRepository.GetByUserAndRange` で取得
  - 日跨ぎ睡眠の捕捉のため睡眠のみ前日から取得する（既存 `GetWeeklyReport` と同様）
  - グリッド範囲: 月初の直前の日曜 〜 月末の直後の土曜
  - 各日セルについて、既存 `BuildDailyReport` の体調平均・睡眠重なり計算ロジックと整合させる
    - 体調平均・気分平均: 当日 `RecordDate` の `HealthRecordEntity` の `(int)Condition` / `(int)Feeling` 平均
    - 睡眠合計: 当日 0:00〜翌0:00 に重なる睡眠時間（本睡眠＋他睡眠）。`OverlapHours` を流用
    - `SleepLevel`: `SleepStandards.Classify(合計時間)`
    - 通所: 当日 `Date` の `ScheduleEntryEntity` から `ScheduleDayClass`（既存 `BuildScheduleParts` と同じ判定: 1件でも通所があれば planned、全部在宅なら remote、なければ rest）と短いサマリ
  - 既存 `BuildDailyReport` / `OverlapHours` / スケジュール判定ロジックは可能な範囲で再利用し、重複ロジックを増やさない

### 4. Controller での構築

- [ ] `Phycock/Controllers/StatisticsController.cs` の `Index` アクションで
  `MonthlyCalendar` を構築して ViewModel に詰める
  - `Phycock/Models/StatisticsViewModels.cs` の `StatisticsViewModel` に
    `MonthlyCalendarDto MonthlyCalendar { get; set; } = new();` を追加
  - `Index` 内で `_service.GetMonthlyCalendar(userId, ws.Year, ws.Month)` を呼ぶ
    （`ws` は既存の正規化済み weekStart）

### 5. View（月次タブ）の実装

- [ ] `Phycock/Views/Statistics/Index.cshtml` の `#monthly` タブを更新する
  - 既存の月次チャート（`monthlyHealthChart`）はカレンダーの下に残す
  - カレンダーグリッド: 7列（日〜土）のテーブルまたは CSS グリッド
  - 各日セル内の表示:
    - 日付（当月外セルは薄く）
    - 体調平均・気分平均: 数値＋背景色（1–5 を赤→緑、null は灰）
    - 睡眠合計: `0.0h` 表示＋ `SleepLevel` による色（不足=赤/やや不足=黄/適正=緑/過剰=青）
    - 通所: `ScheduleDayClass` による色チップ＋短いサマリ
  - 凡例（睡眠4段階の色・体調気分の色レンジ・通所3種）を下部に表示
  - CSS は既存 `<style>` ブロックに追記
- [ ] 同ファイルの週次チャートの睡眠目安帯を 6–8h → 7–9h に更新する
  - `sleepTargetBand` プラグイン呼び出しの `from: 6, to: 8` → `from: 7, to: 9`
  - `chart-guide` の説明文「6-8時間」→「7-9時間」

### 6. 検証

- [ ] `dotnet build Phycock.slnx` が pass する
- [ ] `dotnet test Phycock.slnx` が pass する
- [ ] git add で変更ファイルを個別指定し commit する（`git add -A` 禁止）

## 触ってよい範囲

- `Phycock/Common/SleepStandards.cs`（新規）
- `Phycock/Models/StatisticsViewModels.cs`
- `Phycock/Service/StatisticsService.cs`
- `Phycock/Controllers/StatisticsController.cs`
- `Phycock/Views/Statistics/Index.cshtml`

## 触ってはいけない範囲

- 上記以外のエンティティ・リポジトリ・他コントローラー
- マイグレーション（DB スキーマ変更なし）

## 注意点

- 体調・気分・睡眠の集計は既存 `BuildDailyReport` の計算と数値が一致すること（週次と月次で矛盾しない）
- グリッドは前後月の埋めセルを含むため、`InMonth=false` のセルは色を付けず日付のみ薄く表示
- IDOR 対策は既存 `ResolveTargetUserIdAsync` で担保済み（新規の所有権チェックは不要）
- print モード（`print=1`）は週次のみ。月次タブは print 非対象のまま（既存挙動を維持）
