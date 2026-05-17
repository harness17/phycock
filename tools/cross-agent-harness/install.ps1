param(
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,

    [string]$ProjectName,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

$kitRoot = $PSScriptRoot
$sourceRoot = Resolve-Path (Join-Path $kitRoot "..\..")

if (-not (Test-Path $TargetPath)) {
    New-Item -ItemType Directory -Force -Path $TargetPath | Out-Null
}

$targetRoot = Resolve-Path $TargetPath

if (-not $ProjectName) {
    $ProjectName = Split-Path -Leaf $targetRoot
}

function Copy-HarnessFile {
    param(
        [string]$SourceRelative,
        [string]$TargetRelative,
        [switch]$NoOverwrite
    )

    $source = Join-Path $sourceRoot $SourceRelative
    $target = Join-Path $targetRoot $TargetRelative
    $targetDir = Split-Path -Parent $target

    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

    if ($NoOverwrite -and (Test-Path $target) -and -not $Force) {
        Write-Host "skip existing: $TargetRelative"
        return
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "copied: $TargetRelative"
}

function Write-Template {
    param(
        [string]$TemplateRelative,
        [string]$TargetRelative,
        [switch]$NoOverwrite
    )

    $template = Join-Path $kitRoot $TemplateRelative
    $target = Join-Path $targetRoot $TargetRelative
    $targetDir = Split-Path -Parent $target

    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

    if ($NoOverwrite -and (Test-Path $target) -and -not $Force) {
        Write-Host "skip existing: $TargetRelative"
        return
    }

    $content = Get-Content -Raw -LiteralPath $template
    $content = $content.Replace("{{PROJECT_NAME}}", $ProjectName)
    $content = $content.Replace("{{TARGET_PATH}}", $targetRoot.Path.Replace("\", "/"))

    Set-Content -LiteralPath $target -Value $content -Encoding UTF8
    Write-Host "created: $TargetRelative"
}

Copy-HarnessFile ".claude\rules\cross-agent-harness.md" ".claude\rules\cross-agent-harness.md"
Copy-HarnessFile ".claude\rules\handoff-protocol.md" ".claude\rules\handoff-protocol.md"
Copy-HarnessFile ".claude\skills\codex-handoff\SKILL.md" ".claude\skills\codex-handoff\SKILL.md"
Copy-HarnessFile ".claude\skills\cross-review\SKILL.md" ".claude\skills\cross-review\SKILL.md"
Copy-HarnessFile ".agents\skills\implement-task\SKILL.md" ".agents\skills\implement-task\SKILL.md"

Write-Template "project-collaboration-profile.template.md" ".claude\rules\project-collaboration-profile.md" -NoOverwrite
Write-Template "CLAUDE_CODE_HANDOFF.template.md" "CLAUDE_CODE_HANDOFF.md" -NoOverwrite

Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Fill .claude/rules/project-collaboration-profile.md"
Write-Host "2. Add these lines to CLAUDE.md:"
Write-Host "   @.claude/rules/cross-agent-harness.md"
Write-Host "   @.claude/rules/project-collaboration-profile.md"
Write-Host "   @.claude/rules/handoff-protocol.md"
Write-Host "3. Add a short AGENTS.md section that tells Codex to read CLAUDE_CODE_HANDOFF.md and .agents/skills/implement-task/SKILL.md"
