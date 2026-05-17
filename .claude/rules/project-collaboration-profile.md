# Project Collaboration Profile（Phycock）

`cross-agent-harness.md` を Phycock に適用するためのプロジェクト固有設定。

## プロジェクト

- 名前: Phycock
- 種別: ASP.NET Core MVC + Entity Framework Core + SQL Server の体調管理アプリ
- 主な検証対象: Controller / Service / Repository / Razor View / xUnit / EF Core migration
- 注意領域: 体調・睡眠・通所予定など要配慮データに近い情報、認可、所有者チェック、監査情報

## 担当境界

| 条件 | 振り先 |
|------|--------|
| 単一 Controller / Service / View / Test、仕様明確 | Codex |
| 認可・所有者チェックの追加や回帰テスト | Codex（Claude Code がレビュー） |
| EF Core migration 新設・既存 DB schema の破壊的変更 | Claude Code が設計・migration、Codex は限定範囲の呼び出し側修正 |
| 複数集約や画面遷移をまたぐ設計判断 | Claude Code が先に設計、その後 Codex に分割依頼 |
| UI 変更でブラウザ確認が必要 | 実装は Codex、実動確認は Claude Code |
| GitHub 公開前監査（secrets / 個人情報 / README / license） | Claude Code 主担当、Codex は観点レビュー |

## Verify コマンド

通常のセルフ verify:

```bash
dotnet build Phycock.slnx
dotnet test Phycock.slnx
```

UI 変更、Controller 変更、認証・認可変更では必要に応じて:

```bash
dotnet run --project Phycock
```

ブラウザ確認では、正常系スクリーンショット、バリデーション、認可エラー、既存導線の無破壊を確認する。

## レビュー観点

### 動作

- 完成条件を満たしているか
- 既存機能を壊していないか
- エラー処理・境界値の扱いに穴がないか

### 契約

- Controller action、route、ViewModel、Service interface の契約が呼び出し側と一致しているか
- EF schema 変更があれば migration と DbContext の整合性があるか
- Razor View と POST action の model binding が一致しているか

### テスト

- 完成条件に対応する xUnit テストが追加・更新されているか
- 正常系、異常系、境界値、認可、重複操作を必要に応じて確認しているか
- `dotnet build Phycock.slnx` と `dotnet test Phycock.slnx` が pass するか

### セキュリティ・監査

- 認証と所有者チェックが分離して確認されているか
- CreatedBy / UpdatedBy / CreatedAt / UpdatedAt をクライアント入力に依存していないか
- 体調・睡眠・通所予定などのデータをエラー表示・ログ・URL に不要に出していないか
- 物理削除が必要な場合でも監査性の判断が明記されているか

### スタイル

- 既存の C# / Razor の書き方に揃っているか
- コメントが過剰でないか
- unrelated cleanup や依頼外ファイル変更が混ざっていないか

## Phycock 固有の重大指摘

以下は原則として merge ブロッカーにする。

- IDOR または所有者チェック漏れ
- Admin / Member の権限境界の破壊
- CreatedBy / UpdatedBy / CreatedAt / UpdatedAt の不正な扱い
- 体調・睡眠・通所予定データの不要なログ出力や URL 露出
- migration と DbContext snapshot の不整合
- 物理削除による監査履歴喪失
