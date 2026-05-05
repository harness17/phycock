# =============================================================
# Phycock IIS 初回セットアップスクリプト
# 管理者PowerShellで一度だけ実行してください
# 実行後は Visual Studio から発行するだけでOKです
# =============================================================

#Requires -RunAsAdministrator
$ErrorActionPreference = "Stop"
$publishPath = "C:\inetpub\wwwroot\Phycock"

Write-Host "=== Phycock IIS 初回セットアップ ===" -ForegroundColor Cyan

# 1. 発行フォルダを作成
Write-Host "`n[1/4] 発行フォルダを準備中..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $publishPath | Out-Null
New-Item -ItemType Directory -Force -Path "$publishPath\logs" | Out-Null
Write-Host "  → $publishPath を作成しました" -ForegroundColor Green

# 2. フォルダへの書き込み権限を付与
Write-Host "`n[2/4] フォルダ権限を設定中..." -ForegroundColor Yellow
# VS発行用（現在のWindowsユーザー）
$user = "$env:USERDOMAIN\$env:USERNAME"
icacls $publishPath /grant "${user}:(OI)(CI)M" | Out-Null
# IISアプリプール用
icacls $publishPath /grant "IIS APPPOOL\DefaultAppPool:(OI)(CI)M" | Out-Null
Write-Host "  → $user と DefaultAppPool に書き込み権限を付与しました" -ForegroundColor Green

# 3. LocalDB 共有インスタンスの作成
Write-Host "`n[3/4] LocalDB 共有インスタンスを設定中..." -ForegroundColor Yellow
$shareResult = sqllocaldb share "PhycockLocalDB" "PhycockShared" 2>&1
if ($shareResult -match "already") {
    Write-Host "  → 既に共有済みです（スキップ）" -ForegroundColor Gray
} else {
    Write-Host "  → 共有インスタンス 'PhycockShared' を作成しました" -ForegroundColor Green
}
sqllocaldb start "PhycockLocalDB" 2>&1 | Out-Null

# 4. IIS APPPOOL\DefaultAppPool を DB ユーザーとして登録
Write-Host "`n[4/4] DB ログインを設定中..." -ForegroundColor Yellow
net stop W3SVC 2>&1 | Out-Null
sqlcmd -S "(localdb)\.\PhycockShared" -Q @"
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'IIS APPPOOL\DefaultAppPool')
    CREATE LOGIN [IIS APPPOOL\DefaultAppPool] FROM WINDOWS;
USE PhycockDB;
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'IIS APPPOOL\DefaultAppPool')
BEGIN
    CREATE USER [IIS APPPOOL\DefaultAppPool] FOR LOGIN [IIS APPPOOL\DefaultAppPool];
    ALTER ROLE db_owner ADD MEMBER [IIS APPPOOL\DefaultAppPool];
END
"@
net start W3SVC 2>&1 | Out-Null
Write-Host "  → DB ログインを設定しました" -ForegroundColor Green

Write-Host @"

=== セットアップ完了 ===
次の手順で発行してください：
  1. Visual Studio でプロジェクトを右クリック
  2. 「発行」→ IIS プロファイルを選択して発行
  3. http://localhost/Phycock が自動で開きます

次回以降は手順 1〜3 のみでOKです。
"@ -ForegroundColor Cyan
