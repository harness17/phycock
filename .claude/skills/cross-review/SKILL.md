---
name: cross-review
description: Codex（または Claude Code）が作成した Phycock のコード変更を相互レビューする。`CLAUDE_CODE_HANDOFF.md` の依頼を読み、動作・契約・テスト・セキュリティで確認して結果を追記する。「レビューして」「クロスレビュー」「Codex の作業を確認」と言われたら使う。
---

# /cross-review

共同開発の相互レビュースキル。

## 起動条件

- Codex が実装を完了し、ハンドオフのセルフ verify が ✅ になったとき
- ユーザーが「レビューして」「クロスレビュー」と言ったとき

## レビュー 5 観点

各観点で重大 / 軽微 / 良好を判定する。

### 観点 1: 動作

- 完成条件を満たしているか
- 既存機能を壊していないか
- エラー処理・境界値の扱いに穴がないか

### 観点 2: 契約

- Controller action、route、ViewModel、Service interface の契約が呼び出し側と一致しているか
- EF schema 変更があれば migration と DbContext の整合性があるか
- Razor View と POST action の model binding が一致しているか

### 観点 3: テスト

- 完成条件に対応する xUnit テストが追加・更新されているか
- 正常系、異常系、境界値、認可、重複操作を必要に応じて確認しているか
- `dotnet build Phycock.slnx` と `dotnet test Phycock.slnx` が pass するか

### 観点 4: セキュリティ・監査

- 認証と所有者チェックが分離して確認されているか
- CreatedBy / UpdatedBy / CreatedAt / UpdatedAt をクライアント入力に依存していないか
- 体調・睡眠・通所予定などのデータをエラー表示・ログ・URL に不要に出していないか
- 物理削除が必要な場合でも監査性の判断が明記されているか

### 観点 5: スタイル

- 既存の C# / Razor の書き方に揃っているか
- コメントが過剰でないか
- unrelated cleanup や依頼外ファイル変更が混ざっていないか

## 手順

### 1. ハンドオフを読む

`CLAUDE_CODE_HANDOFF.md` の最新セクションから対象・変更ファイル・完成条件・レビュー観点を確認する。

### 2. 変更ファイルを読む

対象ファイルを順に読み、5 観点でチェックする。

### 3. 既存テストを実行

```bash
dotnet build Phycock.slnx
dotnet test Phycock.slnx
```

失敗するなら重大指摘にする。

### 4. 実動確認が必要ならブラウザ確認

UI 変更、Controller 変更、認証・認可変更を含むなら `dotnet run --project Phycock` で起動し、ブラウザで確認する。

### 5. ハンドオフにレビュー結果を追記

`.claude/rules/handoff-protocol.md` の書式に従い、「### レビュー結果（YYYY-MM-DD, レビュー側）」セクションを該当セクションに追記する。

### 6. ユーザーに報告

- 公開可否（良好 / 修正必須 / 軽微指摘あり）
- 重大指摘の要約
- 次アクション（Codex に修正を返すか、ユーザー merge 指示待ちか）

## 注意

- 認可漏れ・IDOR・監査情報欠落は重大指摘として扱う。
- 動作確認なしで UI 変更を良好にしない。
- 主観的な読みやすさだけの指摘は軽微に留める。
