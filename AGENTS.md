# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## 概要

**Phycock** は ASP.NET Core 10 MVC + SQL Server ベースの体調管理アプリです。[Phycock](https://github.com/your-org/Phycock) テンプレートをベースに開発されます。リタリコワークス通所者向けの実用ツールおよびポートフォリオプロジェクトです。

## セットアップと前提条件

### 初期セットアップ

```bash
# Phycock から複製
git clone https://github.com/your-org/Phycock.git Phycock
cd Phycock

# ネーミング統一（以下を sed/find-replace で実行）
# Phycock → Phycock
# PhycockDB → PhycockDB
# SetApplicationName("Phycock") → SetApplicationName("Phycock")

# 依存パッケージ復元 & ビルド
dotnet restore
dotnet build
```

### 開発環境

- **.NET**: 10.0.0（.NET Core 最新版）
- **IDE**: Visual Studio 2022 / Rider / VS Code
- **DB**: SQL Server LocalDB（開発）/ SQL Server Express / Full Edition（本番候補）
- **言語**: C# 13（Nullable enabled）

## 技術スタック

| 層 | 技術 |
|---|---|
| **フレームワーク** | ASP.NET Core 10 MVC |
| **ORM** | Entity Framework Core 10.0.0 |
| **認証** | ASP.NET Core Identity |
| **UI** | Razor Views + FullCalendar.js + Bootstrap |
| **テスト** | xUnit + Moq |
| **ビルド** | MSBuild（.NET CLI） |

## ビルド・テスト・実行コマンド

```bash
# ビルド
dotnet build

# テスト実行
dotnet test

# ローカル開発サーバー起動（http://localhost:5000）
dotnet run --project Phycock

# EF Core マイグレーション初期化
dotnet ef migrations add Initial --project Phycock --startup-project Phycock
dotnet ef database update --project Phycock --startup-project Phycock
```

## プロジェクト構成

```
Phycock/
├── Phycock/              ← メイン ASP.NET Core MVC アプリケーション
│   ├── Controllers/      ← HTTP エンドポイント（認証・体調記録・スケジュール）
│   ├── Views/            ← Razor テンプレート
│   ├── Models/           ← ViewModel / DTO
│   ├── Entity/           ← EF Core エンティティ
│   ├── Service/          ← ビジネスロジック層
│   ├── Repository/       ← データアクセス層
│   ├── Common/           ← Enum・ユーティリティ
│   ├── Program.cs        ← DI・ミドルウェア設定
│   └── appsettings.json  ← DB 接続文字列・ログ設定
├── CommonLibrary/        ← 共有ライブラリ（ApplicationUser・EntityBase）
├── Tests/                ← xUnit テストプロジェクト
├── docs/                 ← 設計・実装ドキュメント
└── Phycock.sln          ← ソリューション定義
```

## エンティティ設計

### 基底クラス：EntityBase（CommonLibrary）

すべてのドメインエンティティは `EntityBase` を継承し、以下を自動トラッキング：

```csharp
public abstract class EntityBase
{
    public long Id { get; set; }
    public long CreatedBy { get; set; }      // 作成ユーザー ID
    public DateTime CreatedAt { get; set; }  // 作成日時（UTC）
    public long? UpdatedBy { get; set; }     // 最終更新者 ID
    public DateTime? UpdatedAt { get; set; } // 最終更新日時（UTC）
}
```

**体調管理特有の設計指針：**
- 体調記録（HealthRecord）は日付・ユーザー・症状を記録
- CreatedBy/UpdatedBy により監査ログが自動的に残る
- スケジュール（Schedule）は既存 Phycock 機能を活用し、体調チェック予定と統合

### 削除方式

- **物理削除は行わない**（医療関連アプリのため監査ログ要）
- 論理削除カラム `IsDeleted` を追加する場合は手動で実装

## 認証・認可設計

### ロール簡略化

Phycock の ApprovalRequest（承認ワークフロー）は **削除**。以下の構成に簡略化：

```csharp
// 削除対象
// - ApprovalRequest エンティティ
// - ApprovalRequestController
// - ApprovalRequest Views

// 残す
// - Admin ロール（管理者のみスタッフユーザー追加可）
// - Member ロール（通所者）
// - Identity ユーザー管理
```

**権限設計：**

| 操作 | Member | Admin |
|---|---|---|
| 自分の体調記録 CRUD | ✅ | ✅ |
| 他者の体調記録閲覧 | ❌ | ✅ |
| スケジュール参照 | ✅ | ✅ |
| 他者のスケジュール編集 | ❌ | ✅ |
| ユーザー管理 | ❌ | ✅ |

**実装時の注意：**
```csharp
// IDOR（水平権限昇格）対策は必須
[Authorize]
public IActionResult Edit(long healthRecordId)
{
    var userId = _userManager.GetUserId(User);
    var record = await _repo.GetByIdAsync(healthRecordId);
    
    // 所有権チェック
    if (record == null || record.CreatedBy != userId)
        return Forbid();
    
    // ... 編集処理
}
```

## コード規約

### C# ネーミング

```csharp
// クラス・メソッド・プロパティ: PascalCase
public class HealthRecordService { }
public async Task<HealthRecord> GetByIdAsync(long id) { }

// ローカル変数・パラメータ: camelCase
var userId = _userManager.GetUserId(User);
foreach (var item in items) { }

// Private フィールド: _camelCase（先頭アンダースコア）
private readonly IRepository _repo;
```

### エイリアス（競合回避）

```csharp
// SignInResult の競合に注意
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

// 使用時
IdentitySignInResult result = await _signInManager.PasswordSignInAsync(...);
```

## テスト戦略

### テストケース設計

各新機能にはテストケース一覧を先行作成：

```
テストケース例（HealthRecord API）:
1. 正常: 有効な体調記録で登録成功 → HTTP 201 + 記録返却
2. 異常: 必須項目未入力 → HTTP 400 + バリデーションエラー
3. 権限: 未認証 → HTTP 401
4. 権限: 他ユーザーの記録編集試行 → HTTP 403
5. 境界: 0 件取得（ページング） → HTTP 200 + 空配列
```

### テスト実行

```bash
# 全テスト実行
dotnet test

# 特定テストのみ
dotnet test --filter "HealthRecordTests"

# テストカバレッジ確認（別途ツール必要）
# OpenCover / ReportGenerator 活用
```

## DB マイグレーション

### 新しいエンティティ追加時の手順

```bash
# 1. Entity ファイル作成 (Entity/HealthRecord.cs 等)
# 2. DbContext に DbSet 追加
# 3. マイグレーション作成
dotnet ef migrations add Add_HealthRecord --project Phycock --startup-project Phycock

# 4. コード確認後、スキーマ適用
dotnet ef database update --project Phycock --startup-project Phycock

# 5. 本番デプロイ前に確認
# ChangeLog に目を通し、破壊的変更がないか確認
```

## セキュリティ

### 機密データ保護

体調記録は要配慮個人情報に該当する可能性あり。以下を順守：

- **DB 接続文字列**を appsettings.json に硬書きしない（`.local.json` または環境変数）
- **エラーメッセージ**から DB スキーマ・内部情報を露出させない
- **HTTPS リダイレクト**を本番環境で有効化（Program.cs の `app.UseHttpsRedirection()`）
- **CORS ポリシー**を明示的に設定し、許可オリジンを限定

### 入力値検証

- **サーバー側検証必須**（クライアント側は UX 補助のみ）
- 体調データの日付・数値範囲を検証（実装例: `[Range]` DataAnnotation）

## 認可と監査

- Identity の `CreatedBy`/`UpdatedBy` は **サーバー側で自動設定**（クライアント入力は信頼しない）
- `IHttpContextAccessor` で現在ユーザーを取得（Program.cs 設定済み）

```csharp
// Service 層での自動設定例
var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
entity.CreatedBy = long.Parse(userId);
entity.CreatedAt = DateTime.UtcNow;
```

## デプロイ

### 本番環境推奨構成

| 環境 | ホスト | DB | SSL |
|---|---|---|---|
| 開発 | localhost:5000 | LocalDB | 不要 |
| ステージング | Azure App Service / Railway | SQL Server Express | HTTPS 必須 |
| 本番 | Azure App Service / Railway | Azure SQL Database | HTTPS 必須 + HSTS |

### デプロイスクリプト

Phycock の `scripts/deploy-samples.ps1` パターンを参考に、IIS / Azure App Service へのデプロイを自動化。

## ドキュメント参照

- **セットアップ**: `@../Phycock/docs/setup.md`
- **カスタマイズ指針**: `@../Phycock/docs/customization.md`
- **実装パターン**: `@../Phycock/docs/recipes/`
- **認可実装例**: `@../Phycock/AGENTS.md`

## API 設計（将来拡張）

スケジュール API を REST で公開する場合：

- GET `/api/healthRecords` — 自分の記録一覧
- POST `/api/healthRecords` — 新規登録
- GET `/api/healthRecords/{id}` — 詳細取得
- PUT `/api/healthRecords/{id}` — 更新
- DELETE `/api/healthRecords/{id}` — 削除（論理削除）

認証: Bearer Token（JWT 実装検討）。Swagger UI は ApiSample を参考。

## 共同開発ハーネス（Codex × Claude Code）

このリポジトリは Codex と Claude Code が共同で開発する。役割分担・merge ゲート・指摘ラベルは以下のルールに従う。

- `.claude/rules/cross-agent-review.md`
- `.claude/rules/handoff-protocol.md`

**Codex が作業を開始するときの流れ：**

1. `CLAUDE_CODE_HANDOFF.md` の最新セクションを読み、対象・完成条件・触ってよい範囲を確認する。
2. `.agents/skills/implement-task/SKILL.md` に従い、完成条件を再確認してから実装する。
3. 体調・睡眠・通所予定のデータでは、認可・所有者チェック・監査情報・個人情報露出をレビュー観点に含める。
4. `dotnet build Phycock.slnx` と `dotnet test Phycock.slnx` をセルフ verify とする。
5. 実装後は `CLAUDE_CODE_HANDOFF.md` を更新し、Claude Code のレビューに戻す。

**最新の引き継ぎ：** `CLAUDE_CODE_HANDOFF.md` を参照する。

## その他の注意点

- **AutoMapper**: DI 登録時に `ILoggerFactory` が必須（16.x 以上）
- **ServiceFilter**: 使用時は `Program.cs` で事前に DI 登録が必須
- **Razor Runtime Compilation**: 開発環境でのみ有効（本番は無効化）
