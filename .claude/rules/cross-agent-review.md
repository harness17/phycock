# Cross-Agent Review ルール（Phycock 共同開発）

Codex と Claude Code で Phycock を共同開発するときの役割分担・merge ゲート・指摘ラベルを定める。

## 役割分担

| フェーズ | 担当 | 理由 |
|---------|------|------|
| 仕様・設計 | Claude Code | ユーザー対話・業務要件整理の主体 |
| 実装（小〜中タスク） | Codex | 1 タスク完結型の Controller / Service / View / Test 修正に強い |
| 実装（広範囲・横断的） | Claude Code | DB schema・認可契約・複数画面をまたぐ変更の主担当 |
| 単体テスト追加 | Codex（実装と同時） | xUnit の回帰テストを実装と同時に固める |
| 実動確認（`dotnet run`・ブラウザ操作） | Claude Code | Playwright / agent-browser で画面確認を行う |
| レビュー（コード読み） | 相互（作成者の反対側） | 認可・監査・医療寄りデータの扱いの盲点を拾う |
| ハンドオフ更新 | 作業した側 | 完了直後に事実を残す |

## タスク振り分けの判定基準

| 条件 | 振り先 |
|------|--------|
| 単一 Controller / Service / View / Test、仕様明確 | Codex |
| 認可・所有者チェックの追加や回帰テスト | Codex（Claude がレビュー） |
| EF Core migration 新設・既存 DB schema の破壊的変更 | Claude Code が設計・migration、Codex は限定範囲の呼び出し側修正 |
| 複数集約や画面遷移をまたぐ設計判断 | Claude Code が先に設計、その後 Codex に分割依頼 |
| UI 変更でブラウザ確認が必要 | 実装は Codex、実動確認は Claude Code |
| 公開前監査（secrets / 個人情報 / README / license） | Claude Code 主担当、Codex は観点レビュー |

## Merge ゲート 4 条件

`develop` または公開用ブランチへ merge する前に 4 条件すべてが揃っていること。

| # | 条件 | 確認方法 | 担当 |
|---|------|---------|------|
| 1 | セルフ verify | `dotnet build Phycock.slnx` と `dotnet test Phycock.slnx` が pass | 実装者 |
| 2 | 相互レビュー記録 | `CLAUDE_CODE_HANDOFF.md` にレビュー結果が残っている | レビュー側 |
| 3 | 重大指摘なし | レビュー指摘のうち重大ラベルが解消済み | 実装者 |
| 4 | ユーザー merge 指示 | ユーザーが明示的に merge OK と言ったか | ユーザー |

## 指摘ラベル

- 重大: merge ブロッカー。動作不良、認可・所有者チェック漏れ、監査情報欠落、schema 不整合、テスト失敗、既存機能破壊。
- 軽微: 任意。命名、コメント、整形、読みやすさ、文書表現。
- 良好: 問題なし。

## 実動確認ゲート

以下に該当する場合、merge 前に Claude Code によるブラウザ確認を必須とする。

- Razor View / JavaScript / CSS の変更
- Controller action 追加・変更
- 認証・認可・ユーザー管理に関わる変更
- EF Core migration や seed データ変更

確認観点は、正常系スクリーンショット、バリデーション、認可エラー、既存導線の無破壊を最低限とする。
