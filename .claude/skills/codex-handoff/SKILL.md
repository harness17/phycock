---
name: codex-handoff
description: Codex に実装・調査・レビューを依頼する。`project-collaboration-profile.md` と `CLAUDE_CODE_HANDOFF.md` を使って依頼セクションを追記する。「Codex に振って」「Codex でやって」「ハンドオフ作って」と言われたら使う。
---

# /codex-handoff

Codex への作業依頼を作成し、ハンドオフファイルに記録するスキル。

## 起動条件

ユーザーが以下のいずれかを言ったとき：

- 「Codex に振って」「Codex でやって」
- 「ハンドオフ作って」「依頼書作って」
- タスク振り分け基準（`.claude/rules/cross-agent-harness.md` と `.claude/rules/project-collaboration-profile.md`）で Codex 担当と判断したとき

## 手順

### 1. 振り分け確認

タスクが本当に Codex 向きか、`.claude/rules/cross-agent-harness.md` と `.claude/rules/project-collaboration-profile.md` の判定基準で確認する。

Claude Code が握るべきタスクは、先に Claude Code で設計し、Codex には限定された実装・テスト・レビューだけを渡す。

### 2. 依頼内容を整理

以下を確定する：

- 主題（1 行）
- 変更すべきファイル（推定でよい）
- 完成条件（スプリントコントラクト）
- 触ってよい範囲・触ってはいけない範囲
- レビュー観点（Claude Code が後でレビューするときの着眼点）
- verify コマンド（`project-collaboration-profile.md` に従う）

### 3. ブランチ名を提案

`feature/<topic-kebab>` 形式。例：`feature/calendar-month-view`、`feature/admin-registration-validation`。

### 4. `CLAUDE_CODE_HANDOFF.md` に追記

`.claude/rules/handoff-protocol.md` のテンプレに従い、最上部（既存セクションの上）に依頼セクションを追記する。

### 5. Codex に渡す

Codex は `.agents/skills/implement-task/SKILL.md` の手順に従って実装する。

### 6. ユーザーに完了報告

依頼を `CLAUDE_CODE_HANDOFF.md` に追記したこと、Codex が触ってよい範囲、次にレビューする担当を伝える。

## 注意

- 完成条件を曖昧なまま渡さない。
- project profile の重大指摘・レビュー観点を依頼セクションに反映する。
- merge はユーザー明示まで行わない。
