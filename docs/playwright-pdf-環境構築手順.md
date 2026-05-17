# Playwright / PDF出力 環境構築手順

統計の週次レポート PDF 出力は、サーバー側で **Playwright（Chromium ヘッドレス）** を起動し、
アプリ自身のページ（`/Statistics?print=1`）を内部レンダリングして PDF 化する。

このため、**どの環境でも Playwright のブラウザ本体（Chromium）を別途インストールする**必要がある。
NuGet パッケージ `Microsoft.Playwright` を参照しているだけでは動かない。

```
PDF出力リクエスト
  → PdfExportService.RenderPdfAsync()
  → Playwright.Chromium.LaunchAsync()   ← ここで Chromium 実行ファイルが必要
  → 内部URL（Request.Scheme/Host/PathBase から組立）をレンダリング
  → page.PdfAsync() で PDF バイト列を返す
```

---

## 共通の前提

| 項目 | 内容 |
|------|------|
| NuGet | `Microsoft.Playwright`（`Phycock.csproj` に参照済み） |
| ブラウザ本体 | Chromium。`playwright.ps1 install chromium` で取得 |
| 取得先の制御 | 環境変数 `PLAYWRIGHT_BROWSERS_PATH` |
| ビルド成果物 | `bin/<Config>/net10.0/playwright.ps1` がインストーラ |

### `PLAYWRIGHT_BROWSERS_PATH` の値の意味

| 値 | ブラウザ保存先 | 主な用途 |
|----|---------------|---------|
| 未設定 | `%USERPROFILE%\AppData\Local\ms-playwright` | ローカル開発 |
| 固定パス（例 `C:\playwright-browsers`） | そのフォルダ | IIS（複数アカウントで共有） |
| `0` | アプリの出力フォルダ内（`bin\...\.playwright\.local-browsers` 相当） | **Azure・コンテナ（アプリと一緒に配布）** |

> **要点**: IIS のアプリプール ID やクラウドのサンドボックスは、開発者のユーザープロファイルを参照できない。
> 「未設定」のままだとブラウザが見つからず PDF 出力が失敗する。環境に応じて明示的に指定する。

---

## 環境別手順

### 1. ローカル開発（`dotnet run` / Kestrel）

```powershell
# 1. ビルド（playwright.ps1 が出力される）
dotnet build Phycock/Phycock.csproj

# 2. Chromium をインストール（ユーザープロファイルに入る）
pwsh Phycock/bin/Debug/net10.0/playwright.ps1 install chromium

# 3. 起動
dotnet run --project Phycock
```

`PLAYWRIGHT_BROWSERS_PATH` は未設定でよい。実行ユーザーと開発ユーザーが同一のため。

---

### 2. ローカル IIS（in-process ホスティング）

アプリプール ID（既定 `ApplicationPoolIdentity`）は開発者プロファイルを参照できないため、
**共有フォルダ方式**を使う。

```powershell
# 1. 共有フォルダにブラウザをインストール
$env:PLAYWRIGHT_BROWSERS_PATH = "C:\playwright-browsers"
New-Item -ItemType Directory "C:\playwright-browsers" -Force
pwsh <発行先>\playwright.ps1 install chromium

# 2. アプリプール ID に読取+実行権限を付与
icacls "C:\playwright-browsers" /grant "IIS_IUSRS:(OI)(CI)(RX)" /T
```

`web.config` の `<aspNetCore>` に環境変数を渡す（**設定済み**）。

```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="PLAYWRIGHT_BROWSERS_PATH" value="C:\playwright-browsers" />
</environmentVariables>
```

注意点:
- アプリプール ID を専用サービスアカウントに変更した場合は、そのアカウントにも上記権限が必要。
- マシン環境変数で設定する手もあるが管理者権限が要る。web.config 方式はアプリ単位で完結し管理者不要。
- 配置後はアプリプールをリサイクル（または `iisreset`）して環境変数を反映する。

---

### 3. Azure App Service（Windows）

クラウドのサンドボックスにはユーザープロファイルも事前インストールしたブラウザも無い。
**ブラウザをアプリと一緒に配布する**のが最も確実（`PLAYWRIGHT_BROWSERS_PATH=0`）。

#### 3-1. 発行時にブラウザを同梱する

CI / ローカルのどちらでも、publish 後にアプリ出力フォルダへブラウザを入れる。

```powershell
# publish
dotnet publish Phycock/Phycock.csproj -c Release -o ./publish

# publish フォルダ内にブラウザをインストール（0 = 出力フォルダに格納）
$env:PLAYWRIGHT_BROWSERS_PATH = "0"
pwsh ./publish/playwright.ps1 install chromium

# publish フォルダごと Azure へ ZIP デプロイ / GitHub Actions など
```

#### 3-2. App Service のアプリケーション設定（環境変数）

Azure ポータル → App Service → 構成 → アプリケーション設定:

| 名前 | 値 |
|------|-----|
| `PLAYWRIGHT_BROWSERS_PATH` | `0` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__PhycockConnection` | Azure SQL の接続文字列 |

`PLAYWRIGHT_BROWSERS_PATH=0` にすると、アプリは自分の配置フォルダ内のブラウザを参照する。
プロファイル権限の問題が起きない。

#### 3-3. 注意点

- App Service Windows でもプロセス起動数・メモリに制限がある。PDF出力は Chromium を毎回起動するため、
  **Basic (B1) 以上**のプランを推奨（Free/Shared では失敗しやすい）。
- HTTPS のみのバインディングになるため、内部レンダリング URL も `https://` になる。
  自己署名でない正規証明書なら問題ないが、証明書エラーが出る場合は `PdfExportService` の
  `BrowserNewContextOptions` に `IgnoreHTTPSErrors = true` を追加する。
- `web.config` の `PLAYWRIGHT_BROWSERS_PATH` 値（`C:\playwright-browsers`）は **IIS 専用**。
  Azure ではアプリケーション設定側（`0`）が優先されるよう、デプロイスロットの設定を確認する。

---

### 4. Azure App Service（Linux）/ コンテナ

App Service Linux のサンドボックスには Chromium が必要とする OS ライブラリ
（`libnss3`, `libatk`, `libgbm` 等）が無く、`playwright install` だけでは動かない。

→ **カスタム Docker コンテナ**を使う。`Dockerfile` で OS 依存も含めて導入する。

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Playwright が必要とする OS 依存込みで Chromium を導入
# （PowerShell を入れて playwright.ps1 を使う / または dotnet tool 経由）
RUN apt-get update && apt-get install -y powershell \
 && rm -rf /var/lib/apt/lists/*

# ... build ステージで publish した成果物を COPY ...
COPY --from=build /app/publish .

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN pwsh playwright.ps1 install --with-deps chromium

ENTRYPOINT ["dotnet", "Phycock.dll"]
```

ポイント:
- `--with-deps` で OS 依存ライブラリも導入する（Linux のみ有効）。
- `PLAYWRIGHT_BROWSERS_PATH` をイメージ内の固定パスにし、その場所へインストールする。
- このコンテナを Azure Container Apps / App Service for Containers / AKS にデプロイする。

---

## 環境別 設定早見表

| 環境 | ブラウザ導入先 | `PLAYWRIGHT_BROWSERS_PATH` | 設定場所 |
|------|---------------|---------------------------|---------|
| ローカル開発 | ユーザープロファイル | 未設定 | — |
| ローカル IIS | `C:\playwright-browsers`（共有） | `C:\playwright-browsers` | `web.config` |
| Azure App Service (Windows) | アプリ配置フォルダ内 | `0` | App Service アプリ設定 |
| Azure (Linux) / コンテナ | イメージ内固定パス | `/ms-playwright` 等 | `Dockerfile` の `ENV` |

---

## トラブルシューティング

| 症状 | 原因 | 対処 |
|------|------|------|
| PDF出力が 500、ログに `Executable doesn't exist` | ブラウザ未インストール | 環境に応じて `playwright.ps1 install chromium` |
| IIS で 500、ログに `UriFormatException: hostname could not be parsed` | （修正済み）`IServerAddressesFeature` がワイルドカードホストを返す | `StatisticsController.ExportPdf` が現リクエストから URL を組む実装になっているか確認 |
| サブアプリ配置で内部レンダリングが 404 | URL に PathBase（`/Phycock` 等）が無い | 同上。`Request.PathBase` を URL に含める |
| Azure で起動はするが PDF だけ失敗 | プラン不足／プロファイル参照不可 | プランを B1 以上に、`PLAYWRIGHT_BROWSERS_PATH=0` を設定 |
| HTTPS 証明書エラーでレンダリング失敗 | 内部 URL が https かつ証明書不正 | `BrowserNewContextOptions.IgnoreHTTPSErrors = true` |
| Linux で `error while loading shared libraries` | OS 依存ライブラリ不足 | コンテナで `playwright install --with-deps chromium` |

## 確認方法

PDF出力ボタン押下後、HTTP 200 と PDF ファイル（先頭バイト `%PDF`）が返れば成功。
失敗時は Windows イベントログ（アプリケーション、ソース「.NET Runtime」）または
コンテナの標準出力で `ExportPdf` の例外を確認する。
