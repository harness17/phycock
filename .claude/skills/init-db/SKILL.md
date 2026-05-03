---
name: init-db
description: EF Core マイグレーション初期化・データベーススキーマ作成を実行します。初回セットアップ時または新環境構築時に使用してください。
disable-model-invocation: false
---

# /init-db — データベース初期化スキル

新規環境構築時、またはデータベースをリセットする必要がある時に実行します。

## 前提条件

- [ ] .NET 10 SDK がインストール済み
- [ ] SQL Server LocalDB がインストール済み（`sqllocaldb v` で確認）
- [ ] Phycock プロジェクトが `dotnet build` で成功している

## 初期化手順

### 1. 既存マイグレーション確認

```bash
cd Phycock
dotnet ef migrations list --project Phycock --startup-project Phycock
```

**期待される出力例:**
```
20250501120000_Initial
20250502150000_Add_HealthRecord
```

古いマイグレーションが残っている場合は以下を確認：
```bash
# 現在の DB 状態を確認
dotnet ef database info --project Phycock --startup-project Phycock
```

### 2. 初回のみ: マイグレーション作成

**新規プロジェクト時のみ実行（既存プロジェクトはスキップ）:**

```bash
dotnet ef migrations add Initial --project Phycock --startup-project Phycock
```

### 3. データベーススキーマ適用

```bash
dotnet ef database update --project Phycock --startup-project Phycock
```

**期待される出力:**
```
Build started...
Build completed.
Applying migration '20250501120000_Initial'.
Done.
```

### 4. 検証

SQL Server Management Studio または sqlcmd で接続確認：

```bash
sqlcmd -S (localdb)\mssqllocaldb -Q "SELECT name FROM sys.databases WHERE name='PhycockDB'"
```

**期待される出力:**
```
PhycockDB
```

## よくあるエラーと対応

### エラー 1: 「LocalDB がインストールされていない」

```
System.Data.SqlClient.SqlException: SQL Server LocalDB インスタンスが見つかりません。
```

**対応:**
```powershell
# Visual Studio インストーラーから LocalDB 追加、または
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

### エラー 2: 「マイグレーションが適用されていない」

```
There is a pending migration '20250502_Add_HealthRecord'.
```

**対応:**
```bash
# 上記の「3. データベーススキーマ適用」を実行
dotnet ef database update --project Phycock --startup-project Phycock
```

### エラー 3: 「接続文字列が見つからない」

appsettings.json を確認：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PhycockDB;Trusted_Connection=true;"
  }
}
```

### エラー 4: 「DbContext が見つからない」

```bash
# --startup-project フラグを確認
dotnet ef database update --project Phycock --startup-project Phycock
```

## DB リセット（データを初期化する場合）

```bash
# 警告: すべてのデータが削除されます
dotnet ef database drop --project Phycock --startup-project Phycock --force

# その後、再度初期化
dotnet ef database update --project Phycock --startup-project Phycock
```

## 既存 DB へのマイグレーション追加（新エンティティ追加時）

新しいエンティティを追加した後：

```bash
# 1. 新しいマイグレーション作成
dotnet ef migrations add Add_NewEntityName --project Phycock --startup-project Phycock

# 2. 生成されたマイグレーションファイルを確認
# Migrations/20250502_Add_NewEntityName.cs を確認

# 3. 破壊的変更がないか確認後、適用
dotnet ef database update --project Phycock --startup-project Phycock
```

## 本番環境への適用

本番環境では以下を推奨：

1. **ステージング環境で事前テスト**
   ```bash
   dotnet ef database update --project Phycock --startup-project Phycock
   ```

2. **マイグレーション スクリプト出力**
   ```bash
   dotnet ef migrations script --project Phycock --output Migrations/Deploy.sql
   ```

3. **SQL Server DBA に スクリプト確認を依頼**

4. **本番環境で実行**
   ```sql
   -- Deploy.sql を SQL Server Management Studio で実行
   ```

## チェックリスト

- [ ] LocalDB が起動している
- [ ] appsettings.json に DefaultConnection が存在する
- [ ] `dotnet ef database update` が成功した
- [ ] SQL Server で PhycockDB が作成されていることを確認
- [ ] `dotnet run` で アプリケーションが起動する

完了後は `/verify` を実行して、全体検証を行ってください。
