# Cross-Agent Harness Kit

Codex と Claude Code の共同開発ハーネスを別プロジェクトへ入れるための移植キット。

## 使い方

Phycock リポジトリ側から、対象リポジトリを指定して実行する。

```powershell
.\tools\cross-agent-harness\install.ps1 -TargetPath H:\ClaudeCode\OtherProject -ProjectName OtherProject
```

これで次がコピーされる。

- `.claude/rules/cross-agent-harness.md`
- `.claude/rules/handoff-protocol.md`
- `.claude/rules/project-collaboration-profile.md`
- `.claude/skills/codex-handoff/SKILL.md`
- `.claude/skills/cross-review/SKILL.md`
- `.agents/skills/implement-task/SKILL.md`
- `CLAUDE_CODE_HANDOFF.md`

既存の `project-collaboration-profile.md` と `CLAUDE_CODE_HANDOFF.md` は、`-Force` を付けない限り上書きしない。

## 導入後にやること

1. `.claude/rules/project-collaboration-profile.md` を対象プロジェクト用に埋める。
2. `CLAUDE.md` に以下を追加する。

```md
@.claude/rules/cross-agent-harness.md
@.claude/rules/project-collaboration-profile.md
@.claude/rules/handoff-protocol.md
```

3. `AGENTS.md` に以下の要旨を追加する。

```md
## 共同開発ハーネス（Codex × Claude Code）

Codex は `CLAUDE_CODE_HANDOFF.md` の最新セクションを読み、`.agents/skills/implement-task/SKILL.md` と `.claude/rules/project-collaboration-profile.md` に従って作業する。
```

## 運用

- Claude Code から Codex へ振る: `/codex-handoff`
- Codex 側で実装に入る: `CLAUDE_CODE_HANDOFF.md` と `/implement-task`
- 反対側レビュー: `/cross-review`
- merge 前: セルフ verify、相互レビュー、重大指摘なし、ユーザー merge 指示の 4 条件を揃える。
