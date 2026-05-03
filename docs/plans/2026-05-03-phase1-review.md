# Phase 1 レビュー対応 実装計画

**作成日**: 2026-05-03
**対象**: Phase 1 実装済みコードへのレビュー指摘 5件
**設計**: Claude Opus
**プロジェクト**: Phycock — 体調管理アプリ（ASP.NET Core 10 MVC）
**実装状況**: 2026-05-03 Codex 追加実装完了。通常 Schedule はコード・DB とも削除、HealthRecord 症状は `SymptomFlags` へ移行。

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

## 実装方針サマリー（Opus 設計）

| 指摘 | 採用方針 | 理由 |
|------|---------|------|
| Enum 日本語化 | `[Display(Name)]` 直接付与 + `EnumExtensions.GetDisplayName()` 拡張メソッド | 単言語要件。resx は不要 |
| 症状 Enum 化 | `[Flags] enum SymptomType : long` ビットフラグ | 18種で 63bit に収まる。JOIN 不要 |
| Admin ユーザー選択 | GET パラメータ `?targetUserId=xxx`（ページリロード方式） | 既存の filterDate パターンと整合。Ajax より実装シンプル |
| Schedule 削除 | コードのみ削除、DB テーブルは残す | マイグレーションの DropTable を手動で無効化 |

---

## 設計上の重要事項

### ⚠️ セキュリティ: POST 改ざん防止（IDOR）
Member が POST で `model.UserId` を改ざんして他人のレコードを作成できないようにする。

```csharp
// Service.CreateAsync に isAdmin パラメータを追加
entity.UserId = isAdmin ? model.UserId : currentUserId;  // Member は強制上書き
```
HealthRecord / SleepRecord / ScheduleEntry の全 CreateAsync に適用。

### ⚠️ Razor での Enum 日本語化に注意
`@item.RecordTiming` は `ToString()` を返すだけで `[Display]` を読まない。
→ `EnumExtensions.GetDisplayName()` 拡張メソッドを作成し、`@item.RecordTiming.GetDisplayName()` に書き換える。

### ⚠️ `[Flags]` Enum は `Html.GetEnumSelectList` が使えない
症状チェックボックスは `foreach (SymptomType s in Enum.GetValues<SymptomType>())` で手動レンダリング。

### ⚠️ EF Core マイグレーションの DropTable 自動生成
Schedule エンティティを DBContext から削除後に `dotnet ef migrations add` を実行すると、EF Core が ScheduleEvent/ScheduleEventParticipant テーブルの `DropTable` を自動生成する。
→ 生成後に `Up()` / `Down()` の DropTable 文を必ず手動削除してから `database update` を実行。

---

## 実装ステップ（チェックボックス順に実装）

### フェーズ A: Schedule 機能削除

*削除順序: View → ViewModel → Controller → Service → Repository → Entity → Enum → DBContext → DI*

- [ ] `Phycock/Views/Schedule/Index.cshtml` 削除
- [ ] `Phycock/Views/Schedule/_EventFormModal.cshtml` 削除
- [ ] `Phycock/Models/ScheduleViewModels.cs` 削除
- [ ] `Phycock/Controllers/ScheduleController.cs` 削除
- [ ] `Phycock/Service/ScheduleService.cs` 削除
- [ ] `Phycock/Service/ScheduleRecurrenceHelper.cs` 削除
- [ ] `Phycock/Repository/ScheduleRepository.cs` 削除
- [ ] `Phycock/Entity/ScheduleEventEntity.cs` 削除
- [ ] `Phycock/Entity/ScheduleEventParticipantEntity.cs` 削除
- [ ] `Phycock/Entity/Enums/ParticipantStatus.cs` 削除
- [ ] `Phycock/Entity/Enums/RecurrenceType.cs` 削除
- [ ] `Tests/Schedule/ScheduleRecurrenceHelperTests.cs` 削除（フォルダごと）
- [ ] `Phycock/Common/DBContext.cs` の ScheduleEvent 関連 DbSet 3行を削除
- [ ] `Phycock/Program.cs` の ScheduleRepository / ScheduleService DI 登録を削除
- [ ] `Phycock/Views/Shared/_Layout.cshtml` のスケジュールナビリンク削除
- [ ] `dotnet ef migrations add RemoveScheduleEntities` 実行
  - **必須**: 生成された `Up()` / `Down()` の DropTable 文を手動削除
- [ ] `dotnet ef database update` 実行
- [ ] `dotnet build` で通過確認

### フェーズ B: Register Admin 限定化

- [ ] `Phycock/Controllers/AccountController.cs`
  - Register GET / POST の `[AllowAnonymous]` を削除
  - `[Authorize(Roles = "Admin")]` を付与
  - POST: `await _signInManager.SignInAsync(...)` を削除
  - POST: `RedirectToAction("Index", "UserManagement")` に変更
- [ ] `Phycock/Views/Shared/_LoginPartial.cshtml` の「新規登録」リンク削除
- [ ] `Phycock/Views/Account/Login.cshtml` の「新規ユーザー登録はこちら」リンク削除
- [ ] `Phycock/Views/Account/Register.cshtml` の案内文を「管理者がユーザーを登録します」に変更
- [ ] テスト追加（`Tests/Account/AccountControllerTests.cs`）
  - `Register_AsMember_Returns403`
  - `Register_AsAdmin_RedirectsToUserManagement`

### フェーズ C: Enum 日本語表示

- [ ] `Phycock/Common/EnumExtensions.cs`（新規）
  - `GetDisplayName(this Enum value)` 拡張メソッドを実装
  - `[Display(Name)]` 属性があれば Name を返し、なければ `ToString()` にフォールバック
- [ ] `Phycock/Entity/Enums/HealthEnums.cs` に `[Display(Name)]` 追加
  - RecordTiming: `Morning`=起床時、`Noon`=訓練開始時、`Evening`=訓練終了時、`Night`=就眠時
  - ConditionLevel: `VeryBad`=とても悪い、`Bad`=悪い、`Normal`=普通、`Good`=良い、`VeryGood`=とても良い
  - FeelingLevel: 同上
- [ ] `Phycock/Entity/Enums/SleepEnums.cs` に `[Display(Name)]` 追加
  - SleepType: `NightSleep`=本睡眠、`DaytimeNap`=仮眠、`MedicalFacilityRest`=施設での休息、`Other`=その他
- [ ] `Phycock/Entity/Enums/ScheduleEntryEnums.cs` に `[Display(Name)]` 追加
  - ScheduleSession: `AM`=午前、`PM`=午後、`AllDay`=終日
  - ScheduleStatus: `Planned`=予定、`Attended`=通所済み、`Absent`=欠席、`Late`=遅刻、`EarlyLeave`=早退
  - ActivityType: `Program`=プログラム、`IndividualTraining`=個別訓練、`DepartmentActivity`=部署活動、`GoOut`=外出、`Other`=その他
  - ProgramType: `SelfWork`=自分らしく働く、`HealthCare`=ヘルスケア、`WorkplaceCommunication`=職場でのコミュニケーション、`JobHunting`=就職活動、`ApplicationInterview`=応募・面接、`PreWorkPreparation`=就労前の準備、`OtherFreeInput`=その他
- [ ] `Phycock/Views/_ViewImports.cshtml` に `@using Phycock.Common` 追加（拡張メソッド参照用）
- [ ] View 全体を grep（`RecordTiming|ConditionLevel|FeelingLevel|SleepType|ScheduleSession|...`）して `@enum値` を `@enum値.GetDisplayName()` に書き換え
  - `Views/HealthRecord/`: Index.cshtml, Create.cshtml, Edit.cshtml, _Form.cshtml, _DetailPartial.cshtml
  - `Views/SleepRecord/`: 同様
  - `Views/ScheduleEntry/`: Index.cshtml, _FormFields.cshtml 等
  - `Views/Statistics/Index.cshtml`
  - `Views/Home/Index.cshtml`
- [ ] ドロップダウン箇所を `Html.GetEnumSelectList<T>()` に統一（これは `[Display]` を自動で拾う）
- [ ] テスト追加（`Tests/Common/EnumExtensionsTests.cs`）

### フェーズ D: Symptoms Enum 化

- [ ] `Phycock/Entity/Enums/SymptomType.cs`（新規）
  ```
  [Flags] enum SymptomType : long
  None = 0L
  体調面（7種）: Headache=1L, Stomachache=2L, Nausea=4L, Dizziness=8L, Fever=16L, LossOfAppetite=32L, Palpitation=64L
  精神面（6種）: Depression=128L, Anxiety=256L, Irritability=512L, LackOfMotivation=1024L, LackOfConcentration=2048L, Impatience=4096L
  感覚面（5種）: Drowsiness=8192L, Fatigue=16384L, HeavyBody=32768L, MuscleTension=65536L, SensoryOverload=131072L
  ```
  各値に `[Display(Name="頭痛")]` 等を付与
- [ ] `Phycock/Entity/HealthRecordEntity.cs`
  - `string? Symptoms` → `long SymptomFlags` に変更（`[MaxLength(500)]` も削除）
- [ ] `Phycock/Models/HealthRecordViewModels.cs`
  - `SymptomsSelectedList (List<string>)` → `SelectedSymptoms (List<SymptomType>)` に変更
  - `AvailableSymptoms (List<SelectListItem>)` を削除
- [ ] `Phycock/Service/HealthRecordService.cs`
  - `JoinSymptoms/SplitSymptoms` → `ToFlags(List<SymptomType>)` / `FromFlags(long)` に置換
    ```csharp
    static long ToFlags(List<SymptomType> selected) => selected.Aggregate(0L, (acc, s) => acc | (long)s);
    static List<SymptomType> FromFlags(long flags) =>
        Enum.GetValues<SymptomType>().Where(s => s != SymptomType.None && (flags & (long)s) != 0).ToList();
    ```
  - `FillSelections` の AvailableSymptoms 構築ロジックを削除
- [ ] `Phycock/AutoMapperProfiles/HealthRecordProfile.cs`
  - Symptoms のマッピングを `SymptomFlags` 対応に修正（または Service 側で変換するため Ignore）
- [ ] `Phycock/Views/HealthRecord/_Form.cshtml`
  - 症状チェックボックスを手動レンダリングに変更（`Html.GetEnumSelectList` は使わない）
  ```html
  @foreach (var s in Enum.GetValues<SymptomType>().Where(s => s != SymptomType.None))
  {
      <div class="form-check">
          <input type="checkbox" name="SelectedSymptoms" value="@((long)s)" id="symptom_@s"
                 @(Model.SelectedSymptoms.Contains(s) ? "checked" : "") class="form-check-input" />
          <label class="form-check-label" for="symptom_@s">@s.GetDisplayName()</label>
      </div>
  }
  ```
- [ ] `Phycock/Views/HealthRecord/Index.cshtml` の症状表示を `SymptomFlags` からラベル生成に変更
- [ ] `dotnet ef migrations add ChangeSymptomsToFlags` 実行
  - **必須確認**: Up に ScheduleEvent の DropTable が含まれていないか確認・削除
  - **開発 DB 前提（空想定）**: 既存データがある場合は `migrationBuilder.Sql("UPDATE HealthRecord SET SymptomFlags = 0")` を AlterColumn 前に追加
- [ ] `dotnet ef database update` 実行
- [ ] テスト追加: `CreateAsync_SetsSymptomFlags`, `FromFlags_ReturnsCorrectList`

### フェーズ E: Admin ユーザー選択フロー

- [ ] `Phycock/Service/UserManagementService.cs`
  - `GetMemberListAsync()` メソッドを追加（`async Task<List<SelectListItem>>`）
  - `await _userManager.GetUsersInRoleAsync("Member")` → `new SelectListItem { Value = user.Id, Text = user.UserName }` に変換
- [ ] **IDOR 防止（必須）**: HealthRecord / SleepRecord / ScheduleEntry の Service.CreateAsync を修正
  - シグネチャに `bool isAdmin` を追加
  - `entity.UserId = isAdmin ? model.UserId : currentUserId;`（Member は強制上書き）
- [ ] `Phycock/Controllers/HealthRecordController.cs`
  - `Index(DateTime? filterDate)` → `Index(DateTime? filterDate, string? targetUserId)` に変更
  - Admin の場合: `await _userManagementService.GetMemberListAsync()` を呼んで ViewBag に格納
  - Admin の場合: `targetUserId` 指定時はそのユーザーのデータを取得（未指定なら全員 or 選択促す）
  - `Create` GET/POST に `targetUserId` を引き回す
  - `_service.CreateAsync(model, GetCurrentUserId(), User.IsInRole("Admin"))` に更新
- [ ] `Phycock/Models/HealthRecordViewModels.cs`
  - 一覧 ViewModel（または ViewBag）に `TargetUserId (string?)` と `AvailableUsers (List<SelectListItem>)` を追加
  - Form ViewModel に Admin 用 `TargetUserId` フィールド（hidden で引き回し用）を追加
- [ ] `Phycock/Views/HealthRecord/Index.cshtml`
  - `@if (User.IsInRole("Admin"))` ブロックにユーザー選択フォームを追加（GET リクエスト、filterDate も同時送信）
  - 追加ボタンのリンクに `asp-route-targetUserId="@ViewBag.TargetUserId"` を付与
- [ ] SleepRecord (Controller / ViewModel / View) に同様の修正を適用
- [ ] ScheduleEntryController に `targetUserId` を追加
  - `Index(string? targetUserId)` に変更
  - `GetEvents(string start, string end, string? targetUserId)` に変更
  - Admin 以外の場合は `targetUserId` を無視し必ず `GetCurrentUserId()` を使用（認可バイパス防止）
- [ ] `Phycock/Views/ScheduleEntry/Index.cshtml`
  - Admin 向けユーザー選択セレクト追加
  - FullCalendar の events URL を動的化
    ```js
    const targetUserId = document.getElementById('targetUserId')?.value || '';
    events: `/ScheduleEntry/GetEvents?targetUserId=${encodeURIComponent(targetUserId)}`,
    ```
  - セレクト変更時に `calendar.setOption('events', newUrl); calendar.refetchEvents();`
- [ ] テスト追加
  - `CreateAsync_AsMember_IgnoresPostedUserId`（POST 改ざん防止）
  - `GetEvents_AsMember_IgnoresTargetUserId`（認可バイパス防止）

---

## 影響ファイル一覧

### 削除
| ファイル | 理由 |
|----------|------|
| `Controllers/ScheduleController.cs` | 通常スケジュール削除 |
| `Service/ScheduleService.cs` | 同上 |
| `Service/ScheduleRecurrenceHelper.cs` | 同上 |
| `Repository/ScheduleRepository.cs` | 同上 |
| `Entity/ScheduleEventEntity.cs` | 同上 |
| `Entity/ScheduleEventParticipantEntity.cs` | 同上 |
| `Entity/Enums/ParticipantStatus.cs` | 同上 |
| `Entity/Enums/RecurrenceType.cs` | 同上 |
| `Models/ScheduleViewModels.cs` | 同上 |
| `Views/Schedule/Index.cshtml` | 同上 |
| `Views/Schedule/_EventFormModal.cshtml` | 同上 |
| `Tests/Schedule/ScheduleRecurrenceHelperTests.cs` | 同上 |

### 修正（26ファイル）
`Common/DBContext.cs` / `Program.cs` / `Views/Shared/_Layout.cshtml` / `Views/Shared/_LoginPartial.cshtml` / `Views/Account/Login.cshtml` / `Views/Account/Register.cshtml` / `Views/_ViewImports.cshtml` / `Controllers/AccountController.cs` / `Controllers/HealthRecordController.cs` / `Controllers/SleepRecordController.cs` / `Controllers/ScheduleEntryController.cs` / `Service/HealthRecordService.cs` / `Service/SleepRecordService.cs` / `Service/ScheduleEntryService.cs` / `Service/UserManagementService.cs` / `Entity/HealthRecordEntity.cs` / `Entity/Enums/HealthEnums.cs` / `Entity/Enums/SleepEnums.cs` / `Entity/Enums/ScheduleEntryEnums.cs` / `Models/HealthRecordViewModels.cs` / `Views/HealthRecord/*.cshtml（5ファイル）` / `Views/SleepRecord/*.cshtml（5ファイル）` / `Views/ScheduleEntry/*.cshtml（4ファイル）` / `Views/Statistics/Index.cshtml` / `Views/Home/Index.cshtml` / `AutoMapperProfiles/HealthRecordProfile.cs`

### 新規（6ファイル）
| ファイル | 内容 |
|----------|------|
| `Common/EnumExtensions.cs` | `GetDisplayName()` 拡張メソッド |
| `Entity/Enums/SymptomType.cs` | `[Flags] enum SymptomType : long`（18種） |
| `Migrations/*_RemoveScheduleEntities.cs` | 空の Up/Down（DropTable を手動削除） |
| `Migrations/*_ChangeSymptomsToFlags.cs` | `SymptomFlags (long)` への型変更 |
| `Tests/Account/AccountControllerTests.cs` | Register Admin 限定テスト |
| `Tests/Common/EnumExtensionsTests.cs` | GetDisplayName テスト |

---

## リスクと対処

| # | リスク | 対処 |
|---|--------|------|
| R1 | EF Core が DropTable を自動生成 | マイグレーション生成後に Up/Down の DropTable 文を手動削除してから `database update` |
| R2 | Member が POST 改ざんで他人のレコードを作成 | Service.CreateAsync で isAdmin=false の場合 currentUserId で強制上書き（IDOR 防止） |
| R3 | Razor の `@enum値` が日本語にならない | `EnumExtensions.GetDisplayName()` に全 View を書き換え。grep で漏れを確認 |
| R4 | `[Flags]` Enum で Html.GetEnumSelectList が破綻 | 症状は手動チェックボックスレンダリング |
| R5 | 既存 HealthRecord データの型変換エラー | 空 DB 前提。データがある場合は `migrationBuilder.Sql("UPDATE HealthRecord SET SymptomFlags = 0")` を AlterColumn 前に追加 |
| R6 | ScheduleEntry の GetEvents 認可バイパス | Admin 以外は targetUserId を無視、必ず GetCurrentUserId() を使用 |
| R7 | マイグレーション順序の混在 | フェーズ A の RemoveScheduleEntities → フェーズ D の ChangeSymptomsToFlags の順で作成・適用 |

### フェーズ F: CommonLibrary HtmlHelper 拡張メソッド移植

**方針 A**: DevNet（System.Web.Mvc）の HtmlHelper 拡張を ASP.NET Core（IHtmlHelper）スタイルで移植。

**API 対応表:**

| DevNet | DevNext (ASP.NET Core) |
|--------|----------------------|
| `HtmlHelper<TModel>` | `IHtmlHelper<TModel>` |
| `MvcHtmlString` | `IHtmlContent` / `HtmlString` |
| `ModelMetadata.FromLambdaExpression(expr, viewData)` | `ExpressionMetadataProvider.FromLambdaExpression(expr, viewData, metadataProvider)` |
| `ExpressionHelper.GetExpressionText(expr)` | `ExpressionHelper.GetExpressionText(expr)` (同名、Microsoft.AspNetCore.Mvc.ViewFeatures) |
| `tagBuilder.ToString(TagRenderMode.SelfClosing)` | `tagBuilder.RenderSelfClosingTag()` → `IHtmlContentBuilder` 経由 |
| `helper.Partial(name, model, viewData)` | `await helper.PartialAsync(name, model, viewData)` |
| `HttpUtility.HtmlEncode` | `HtmlEncoder.Default.Encode` |

- [ ] `CommonLibrary/Extensions/Helper/` フォルダを新規作成
- [ ] `CommonLibrary/Extensions/Helper/HtmlExtensions.cs`（新規）
  - `FormatNewLines(this IHtmlHelper helper, string text)` → `IHtmlContent`
  - `DisplayForEnum<TModel, TProperty>(this IHtmlHelper<TModel>, Expression<Func<TModel, TProperty>>)` → `IHtmlContent`
    - `EnumUtility.GetDescription` で表示名取得、現在値と一致する項目を `<label>` でラップ
  - `PartialFor<TModel, TProperty>(this IHtmlHelper<TModel>, Expression, string partialViewName)` → `IHtmlContent`
    - HtmlFieldPrefix を `expression` のパス名で構築して `Partial()` を呼ぶ
  - `PartialFor<TModel>(this IHtmlHelper<TModel>, string prefix, string partialViewName)` → `IHtmlContent`
- [ ] `CommonLibrary/Extensions/Helper/HtmlExtensionsForCheckBox.cs`（新規）
  - `CheckBoxForSelectList<TModel, TProperty>(this IHtmlHelper<TModel>, Expression, IEnumerable<SelectListItem>, ...)` where `TProperty : List<string>`
    - `TagBuilder("input")` → `tagBuilder.TagRenderMode = TagRenderMode.SelfClosing`
    - モデル値の checked 判定: `((TProperty)metaData.Model).Any(x => x == item.Value)`
    - チェック無し時の hidden input を各チェックボックス直後に挿入
  - `CheckBoxForValue<TModel, TProperty>(this IHtmlHelper<TModel>, Expression, SelectListItem, int, ...)` where `TProperty : List<string>`
  - `CreateHtmlAttribute(TagBuilder, object)` ヘルパー（private static）
- [ ] `CommonLibrary/Extensions/Helper/HtmlExtensionsForRadioButton.cs`（新規）
  - `RadioButtonForEnum<TModel, TProperty>(this IHtmlHelper<TModel>, Expression, string prefix, string clickevent, bool orderDecFlag)` → `IHtmlContent`
    - `htmlHelper.RadioButtonFor(expression, name, new { id, onclick })` を呼ぶ
    - `orderDecFlag=true` の場合は `Enum.GetNames().Reverse()` で逆順
  - `RadioButtonForSelectList<TModel, TProperty>(this IHtmlHelper<TModel>, Expression, IEnumerable<SelectListItem>, ...)` → `IHtmlContent`
    - 選択状態の判定: `fullName + "_" + metaData.Model.ToString() == id`
- [ ] `CommonLibrary/CommonLibrary.csproj` に不足パッケージ確認（`Microsoft.AspNetCore.App` フレームワーク参照で十分）
- [ ] `Phycock/Views/_ViewImports.cshtml` に `@using Dev.CommonLibrary.Extensions.Helper` を追加
- [ ] `dotnet build` で通過確認

---

## 実装前の最終チェックリスト
- [ ] 各フェーズ完了時に `dotnet build` + `dotnet test` を実行
- [ ] マイグレーション実行前に `dotnet ef migrations script` でプレビューして DropTable がないか確認
- [ ] フェーズ E 完了後に agent-browser で動作確認
  - /Schedule → 404
  - /Account/Register（Member ロール）→ 403
  - /HealthRecord/Index（Admin）→ ユーザー選択 UI が表示される
  - 体調記録フォームの各選択肢が日本語表示される
