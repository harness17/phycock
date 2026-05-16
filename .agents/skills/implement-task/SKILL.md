---
name: implement-task
description: Codex が Phycock のハンドオフ依頼を受けて実装に入るときに使う。ブランチ作成・実装・セルフ verify・ハンドオフ更新の一連の流れを定義する。
---

# /implement-task（Codex 側）

`CLAUDE_CODE_HANDOFF.md` に書かれた依頼を読んで実装に入るスキル。

## 起動条件

- Codex が起動された直後、最初に `CLAUDE_CODE_HANDOFF.md` を読んだとき
- ユーザーから「実装して」「進めて」と言われたとき

## 手順

### 1. ハンドオフ依頼を読む

`CLAUDE_CODE_HANDOFF.md` 最上部のセクションを読み、以下を確認する：

- 自分宛て（Codex）の依頼か
- 主題と完成条件
- 触ってよい範囲・触ってはいけない範囲
- 提案されたブランチ名
- 認可・監査・個人情報に関するレビュー観点

### 2. ブランチを切る

原則として `develop` から `feature/<topic>` を切る。既に作業ブランチが指定されている場合はそれに従う。

```bash
git checkout develop
git pull origin develop
git checkout -b <提案されたブランチ名>
```

Codex 環境で git lock の権限エラーが出る場合は、実装と verify まで進め、commit / merge は Claude Code に戻す。

### 3. 完成条件を再確認

スプリントコントラクト形式で完成条件を書き出す。最低限、通常動作、認可・利用前提、エラー処理、非回帰確認を含める。

完成条件が曖昧な場合はユーザーに確認する。

### 4. 実装

- 触ってよい範囲だけを編集する。
- Controller / Service / Repository / ViewModel / Razor View の既存パターンに合わせる。
- 認証済みであることと、対象データの所有者であることを別々に確認する。
- CreatedBy / UpdatedBy / 日時はサーバー側で扱い、クライアント入力を信頼しない。
- テストが必要な変更では xUnit テストを追加・更新する。

### 5. セルフ verify

```bash
dotnet build Phycock.slnx
dotnet test Phycock.slnx
```

UI 変更や Controller 変更では、必要に応じて `dotnet run --project Phycock` とブラウザ確認を行う。

### 6. ハンドオフを更新

`CLAUDE_CODE_HANDOFF.md` の該当セクションを編集する：

- セルフ verify を ✅ に更新
- 実装内容の概要を追記
- 実動確認の有無を追記
- 次アクションを「Claude Code によるレビュー」に更新

### 7. ターンを返す

ユーザーに「実装完了、レビューを Claude Code にお願いしたい」と報告する。ユーザーが明示していない限り、develop / main / master へ merge しない。

## 禁止事項

- `develop` / `main` / `master` へ直接 commit・push しない。
- `git add -A` / `git add .` を使わない。
- 完成条件にない機能を勝手に追加しない。
- ハンドオフの「触ってはいけない範囲」に手を出さない。
- 体調・睡眠・通所予定の具体データや個人情報をログ・エラー・サンプルに入れない。
