# Phase 1 レビュー対応 実装計画

**作成日**: 2026-05-03  
**対象**: Phase 1 実装済みコードへのレビュー指摘 5件  
**設計**: Claude Opus  
**プロジェクト**: Phycock — 体調管理アプリ（ASP.NET Core 10 MVC）

| フェーズ | 内容 | 実施者 | 状態 |
|---------|------|-------|------|
| A〜E | レビュー指摘対応 | Codex | ✅ 完了（2026-05-03） |
| レビュー修正 | コードレビュー指摘3件 | ClaudeCode | ✅ 完了（2026-05-03） |
| F | CommonLibrary HtmlHelper 移植 | 未着手 | ⬜ 未着手 |

---

## スプリントコントラクト（完成条件）

**正常系:**
- [x] Admin が /Account/Register から新規ユーザーを登録でき、登録後は UserManagement/Index にリダイレクトされる
- [x] Admin が /HealthRecord/Index でユーザー選択セレクトボックスから Member を選ぶと、そのユーザーの記録が表示される
- [x] Member が /HealthRecord/Index にアクセスすると自分の記録のみ表示される（ユーザー選択 UI は非表示）
- [x] 体調記録フォームの各 Enum フィールド（RecordTiming・ConditionLevel・FeelingLevel・SymptomType 等）が日本語で表示される
- [x] 症状選択がチェックボックス群（SymptomType Enum）で表示され、保存・再表示が正しく動作する
- [x] /Schedule/* へのアクセスで 404 が返る（削除後）

**認可:**
- [x] 未認証ユーザーは Register にアクセスできない（ログインページにリダイレクト）
- [x] Member ロールのユーザーは Register にアクセスできない（403）

**異常系:**
- [x] Schedule 機能削除後もビルドが通る（参照が全て除去されている）
- [x] Symptoms 型変更後もマイグレーション適用でビルド・動作が維持される

**副作用:**
- [x] ScheduleEntry（通所スケジュール）の機能は引き続き動作する
- [x] 既存の HealthRecord/SleepRecord/Statistics のビルド・動作が維持される

---

## 実装方針サマリー（Opus 設計 → Codex 実装差分）

| 指摘 | 設計方針 | 実際の実装 |
|------|---------|-----------|
| Enum 日本語化 | `[Display(Name)]` + `GetDisplayName()` | 設計通り。`Phycock/Common/EnumExtensions.cs` に実装 |
| 症状 Enum 化 | `[Flags] enum SymptomType : long` ビットフラグ | 設計通り。18種、`SymptomFlags (bigint)` カラム |
| Admin ユーザー選択 | GET パラメータ `?targetUserId=xxx`（ページリロード方式） | **変更**: セッション管理方式に変更。ヘッダーの `_TargetUserSelectorPartial` で全画面共通セレクタ、`TargetUserController` + `UserManagementService.Get/SetSelectedMemberUserIdAsync` で管理 |
| Schedule 削除 | コードのみ削除、DB テーブルは残す | **変更**: DB テーブルも削除（ユーザー明示指示）。`RemoveScheduleEntities` マイグレーションで `DropTable` を適用 |

---

## 設計上の重要事項（実装済みメモ）

### ✅ セキュリティ: POST 改ざん防止（IDOR）実装済み

```csharp
// Service.Create に isAdmin パラメータ実装済み
entity.UserId = isAdmin && !string.IsNullOrWhiteSpace(model.UserId) ? model.UserId : currentUserId;
```
HealthRecord / SleepRecord / ScheduleEntry の全 `Create` に適用済み。

### ✅ セッション管理方式のユーザー選択

ヘッダーの `_TargetUserSelectorPartial` を各 Controller が `ResolveTargetUserIdAsync()` で参照。
Member は無効化（LockoutEnd=9999-12-31）されていると候補から除外される。

### ✅ ユーザー管理の追加仕様（計画外・Codex 追加実装）

- `UserName` をログイン ID と分離（登録フォームに追加）
- `RequireUniqueEmail = true` に変更
- ロールは兼任なし（ラジオ1択）
- ユーザー無効化 = LockoutEnabled + LockoutEnd=9999-12-31 設定（物理削除なし）
- 初期 Admin（Id="1"）は無効化・ロール変更不可

### ✅ 睡眠記録の翌日跨ぎ対応

`SleepRecordService.BuildSleepDateTimes` で `endDate <= startDate` の場合 `+1日`。

---

## 実装ステップ（完了済み）

### フェーズ A: Schedule 機能削除 ✅

- [x] `Phycock/Views/Schedule/` 削除（Index.cshtml, _EventFormModal.cshtml）
- [x] `Phycock/Models/ScheduleViewModels.cs` 削除
- [x] `Phycock/Controllers/ScheduleController.cs` 削除
- [x] `Phycock/Service/ScheduleService.cs` / `ScheduleRecurrenceHelper.cs` 削除
- [x] `Phycock/Repository/ScheduleRepository.cs` 削除
- [x] `Phycock/Entity/ScheduleEventEntity.cs` / `ScheduleEventParticipantEntity.cs` 削除
- [x] `Phycock/Entity/Enums/ParticipantStatus.cs` / `RecurrenceType.cs` 削除
- [x] `Tests/Schedule/` 削除（フォルダごと）
- [x] `Phycock/Common/DBContext.cs` の ScheduleEvent 関連 DbSet 削除
- [x] `Phycock/Program.cs` の ScheduleRepository / ScheduleService DI 登録削除
- [x] `Phycock/Views/Shared/_Layout.cshtml` のスケジュールナビリンク削除
- [x] `dotnet ef migrations add RemoveScheduleEntities` 実行 → DB テーブルも `DropTable` 適用
- [x] `dotnet ef database update` 実行
- [x] `dotnet build` 通過確認

> **注記**: 計画では「DBテーブルは残す（DropTable を手動削除）」だったが、
> ユーザー指示「Schedule系テーブルは必要ない」により DropTable も適用。

### フェーズ B: Register Admin 限定化 ✅

- [x] `AccountController.cs` — Register GET/POST の `[Authorize(Roles = "Admin")]` 付与、`[AllowAnonymous]` 削除
- [x] 登録後は `RedirectToAction("Index", "UserManagement")` にリダイレクト
- [x] `_LoginPartial.cshtml` の「新規登録」リンク削除
- [x] `Views/Account/Login.cshtml` の「新規ユーザー登録はこちら」リンク削除
- [x] `Views/Account/Register.cshtml` の案内文変更
- [x] `RegisterViewModel` に `UserName` フィールド追加（Codex 追加仕様）
- [x] `IdentityOptions.User.RequireUniqueEmail = true` 設定（Codex 追加仕様）
- [x] テスト追加（`Tests/Account/AccountControllerTests.cs`）
  - `Register_Get_IsAdminOnly` / `Register_Post_IsAdminOnly`
  - `RegisterViewModel_RequiresUserName`
  - `IdentityOptions_RequireUniqueEmail_IsEnabled`

### フェーズ C: Enum 日本語表示 ✅

- [x] `Phycock/Common/EnumExtensions.cs`（新規）— `GetDisplayName(this Enum value)` 実装
- [x] `Phycock/Entity/Enums/HealthEnums.cs` に `[Display(Name)]` 追加
  - RecordTiming: 起床時 / 訓練開始時 / 訓練終了時 / 就眠時
  - ConditionLevel / FeelingLevel: とても悪い 〜 とても良い（5段階）
- [x] `Phycock/Entity/Enums/SleepEnums.cs` に `[Display(Name)]` 追加
- [x] `Phycock/Entity/Enums/ScheduleEntryEnums.cs` に `[Display(Name)]` 追加
- [x] `Phycock/Views/_ViewImports.cshtml` に `@using Phycock.Common` 追加
- [x] 全 View を `.GetDisplayName()` 呼び出しに書き換え
- [x] ドロップダウンは `Html.GetEnumSelectList<T>()` を使用（[Display] を自動で拾う）
- [x] テスト追加（`Tests/Common/EnumDisplayExtensionsTests.cs`）

### フェーズ D: Symptoms Enum 化 ✅

- [x] `Phycock/Entity/Enums/SymptomType.cs`（新規）— `[Flags] enum SymptomType : long`（18種）
- [x] `HealthRecordEntity.cs` — `string? Symptoms` → `long SymptomFlags` に変更
- [x] `HealthRecordViewModels.cs` — `SelectedSymptoms (List<SymptomType>)` に変更
- [x] `HealthRecordService.cs` — `ToFlags` / `FromFlags` 実装
- [x] `Views/HealthRecord/_Form.cshtml` — 症状チェックボックス手動レンダリング
- [x] `Views/HealthRecord/Index.cshtml` — 症状表示を `SymptomFlags` からラベル生成
- [x] マイグレーション `RemoveScheduleEntities` に SymptomFlags 変更を統合済み
- [x] テスト追加: `Create_SetsSymptomFlags` / `FromFlags_ReturnsCorrectList`

> **注記**: 計画では `ChangeSymptomsToFlags` を別マイグレーションとしていたが、
> `RemoveScheduleEntities` に統合された。

### フェーズ E: Admin ユーザー選択フロー ✅

- [x] `UserManagementService.GetMemberListAsync()` 実装（無効化ユーザー除外）
- [x] `UserManagementService.Get/SetSelectedMemberUserIdAsync()` 実装（セッション管理）
- [x] `Phycock/Controllers/TargetUserController.cs`（新規）— `[Authorize(Roles = "Admin")]`
- [x] `Views/Shared/_TargetUserSelectorPartial.cshtml`（新規）— ヘッダー共通セレクタ
- [x] **IDOR 防止**: HealthRecord / SleepRecord / ScheduleEntry の `Service.Create` を修正
- [x] HealthRecord / SleepRecord / ScheduleEntry Controller に `ResolveTargetUserIdAsync()` 追加
- [x] テスト追加:
  - `Create_AsMember_IgnoresPostedUserId`（POST 改ざん防止）

> **注記**: 計画の「GET パラメータ `?targetUserId=xxx`」方式ではなく、
> 「セッション管理 + ヘッダー共通セレクタ」方式に変更。画面間で操作対象を維持できる。

---

## ClaudeCode コードレビュー修正（2026-05-03）

ビルド・74テスト通過を確認した上で、以下3件を修正・コミット（`e6b3a78`）。

- [x] **[1] `selected="False"` HTML バグ修正**  
  `_TargetUserSelectorPartial.cshtml` / `_IndexPartial.cshtml` の  
  `selected="@(条件)"` → `@(条件 ? "selected" : "")` に変更  
  （HTML Boolean 属性は値を問わず存在で「選択中」扱いになるため）

- [x] **[2] デッドコード削除**  
  `Views/HealthRecord/_DetailPartial.cshtml` — どこからも参照されていない  
  （Index.cshtml のモーダルは JavaScript で直接 HTML 構築）

- [x] **[3] 同期メソッドの `Async` サフィックス除去**  
  `GetListAsync`→`GetList`、`CreateAsync`→`Create` 等 15種を一括リネーム  
  対象: HealthRecordService / SleepRecordService / ScheduleEntryService /  
  StatisticsService / DashboardService およびそれらの Controller・Test

---

## フェーズ F: CommonLibrary HtmlHelper 拡張メソッド移植（未着手）

**方針 A**: DevNet（System.Web.Mvc）の HtmlHelper 拡張を ASP.NET Core（IHtmlHelper）スタイルで移植。

**API 対応表:**

| DevNet | DevNext (ASP.NET Core) |
|--------|----------------------|
| `HtmlHelper<TModel>` | `IHtmlHelper<TModel>` |
| `MvcHtmlString` | `IHtmlContent` / `HtmlString` |
| `ModelMetadata.FromLambdaExpression(expr, viewData)` | `ExpressionMetadataProvider.FromLambdaExpression(expr, viewData, metadataProvider)` |
| `ExpressionHelper.GetExpressionText(expr)` | `ExpressionHelper.GetExpressionText(expr)` (同名、Microsoft.AspNetCore.Mvc.ViewFeatures) |
| `tagBuilder.ToString(TagRenderMode.SelfClosing)` | `tagBuilder.TagRenderMode = TagRenderMode.SelfClosing` → `WriteTo` 経由 |
| `helper.Partial(name, model, viewData)` | `await helper.PartialAsync(name, model, viewData)` |
| `HttpUtility.HtmlEncode` | `HtmlEncoder.Default.Encode` |

- [ ] `CommonLibrary/Extensions/Helper/` フォルダを新規作成
- [ ] `CommonLibrary/Extensions/Helper/HtmlExtensions.cs`（新規）
  - `FormatNewLines(this IHtmlHelper helper, string text)` → `IHtmlContent`
  - `DisplayForEnum<TModel, TProperty>(this IHtmlHelper<TModel>, Expression<Func<TModel, TProperty>>)` → `IHtmlContent`
  - `PartialFor<TModel, TProperty>(this IHtmlHelper<TModel>, Expression, string partialViewName)` → `IHtmlContent`
  - `PartialFor<TModel>(this IHtmlHelper<TModel>, string prefix, string partialViewName)` → `IHtmlContent`
- [ ] `CommonLibrary/Extensions/Helper/HtmlExtensionsForCheckBox.cs`（新規）
  - `CheckBoxForSelectList<TModel, TProperty>` where `TProperty : List<string>`
  - `CheckBoxForValue<TModel, TProperty>` where `TProperty : List<string>`
  - `CreateHtmlAttribute(TagBuilder, object)` ヘルパー（private static）
- [ ] `CommonLibrary/Extensions/Helper/HtmlExtensionsForRadioButton.cs`（新規）
  - `RadioButtonForEnum<TModel, TProperty>` — `orderDecFlag=true` で逆順対応
  - `RadioButtonForSelectList<TModel, TProperty>`
- [ ] `Phycock/Views/_ViewImports.cshtml` に `@using Dev.CommonLibrary.Extensions.Helper` を追加
- [ ] `dotnet build` で通過確認

---

## 最終状態チェックリスト

- [x] `dotnet build` 0 error / 0 warning
- [x] `dotnet test` 74 tests passed
- [x] `/Schedule/Index` → 404
- [x] `/Account/Register`（未認証）→ 302（ログインへリダイレクト）
- [x] `/HealthRecord/Index`（未認証）→ 302
- [x] Enum 選択肢が日本語で表示される
- [x] SymptomFlags ビットフラグで保存・復元される
- [x] 翌日跨ぎ睡眠（22:30〜06:15）が正しく保存される
- [x] Admin の対象利用者セレクタがヘッダーに表示される
- [x] 無効化ユーザーが対象利用者候補から除外される
- [ ] フェーズ F（HtmlHelper 移植）完了後に `dotnet build` 再確認
