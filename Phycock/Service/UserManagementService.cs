using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Phycock.Common;
using Dev.CommonLibrary.Entity;
using Phycock.Entity;
using Phycock.Models;

namespace Phycock.Service
{
    /// <summary>
    /// ユーザー・ロール管理サービス
    /// Identity の UserManager / RoleManager を使ってユーザー情報を操作する
    /// </summary>
    public class UserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SelectedMemberUserIdSessionKey = "SelectedMemberUserId";
        public static readonly DateTimeOffset DisabledLockoutEnd =
            new(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc));

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 検索条件・ページング・ソートに基づきユーザー一覧を取得する
        /// </summary>
        public async Task<UserManagementViewModel> GetUserListAsync(UserManagementViewModel model)
        {
            if (model.Cond == null) model.Cond = new UserManagementCondViewModel();
            // ページャー設定（件数・ページ番号・ソート列 を CondViewModel に反映）
            LocalUtil.SetPager(model.Cond, model);

            var query = _userManager.Users.AsNoTracking().AsQueryable();

            // 検索条件フィルタリング
            if (!string.IsNullOrEmpty(model.Cond.UserName))
                query = query.Where(u => u.UserName != null && u.UserName.Contains(model.Cond.UserName));

            if (!string.IsNullOrEmpty(model.Cond.Email))
                query = query.Where(u => u.Email != null && u.Email.Contains(model.Cond.Email));

            // ソート
            query = model.Sort switch
            {
                "Email"    => model.SortDir == "ASC" ? query.OrderBy(u => u.Email)    : query.OrderByDescending(u => u.Email),
                "UserName" => model.SortDir == "ASC" ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName),
                _          => query.OrderBy(u => u.UserName),
            };

            // 件数取得（ページングより先に全件数を確定する）
            int totalRecords = await query.CountAsync();

            // ページング適用
            LocalUtil.SetTakeSkip(ref query, model.Cond);
            var users = await query.ToListAsync();

            // ポイント: GetRolesAsync は非同期・1件ずつのため foreach で処理する
            var items = new List<UserManagementListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserManagementListItemViewModel
                {
                    Id             = user.Id,
                    UserName       = user.UserName ?? "",
                    Email          = user.Email ?? "",
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd     = user.LockoutEnd,
                    IsDisabled     = IsDisabled(user),
                    EmailConfirmed = user.EmailConfirmed,
                    Roles          = roles.ToList(),
                });
            }

            model.RowData = new UserManagementDataViewModel
            {
                rows    = items,
                Summary = Util.CreateSummary(model.Cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示"),
            };

            return model;
        }

        /// <summary>
        /// 指定IDのユーザーを編集画面用 ViewModel に変換して返す
        /// </summary>
        public async Task<UserManagementEditViewModel?> GetUserEditAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var currentRoles   = await _userManager.GetRolesAsync(user);
            var availableRoles = GetAvailableRoles();

            return new UserManagementEditViewModel
            {
                Id             = user.Id,
                UserName       = user.UserName ?? "",
                Email          = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                RoleName       = currentRoles.FirstOrDefault() ?? "",
                RoleNames      = currentRoles.ToList(),
                AvailableRoles = availableRoles,
            };
        }

        /// <summary>
        /// ユーザーの基本情報とロールを更新する
        /// </summary>
        public async Task<IdentityResult> UpdateUserAsync(UserManagementEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "ユーザーが見つかりません。" });

            var selectedRole = model.RoleName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(selectedRole))
                return IdentityResult.Failed(new IdentityError { Description = "ロールは1つ選択してください。" });

            var roleExists = await _roleManager.RoleExistsAsync(selectedRole);
            if (!roleExists)
                return IdentityResult.Failed(new IdentityError { Description = "存在しないロールは選択できません。" });

            if (user.Id == Const.SystemAdminUserId && selectedRole != ApplicationRoleType.Admin.ToString())
                return IdentityResult.Failed(new IdentityError { Description = "初期管理者ユーザーの Admin ロールは変更できません。" });

            user.UserName       = model.UserName;
            user.Email          = model.Email;
            user.EmailConfirmed = model.EmailConfirmed;
            user.ApplicationRoleName = selectedRole;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return updateResult;

            // ポイント: 今回は兼任を扱わないため、ロールは常に1件へ収束させる
            var currentRoles  = await _userManager.GetRolesAsync(user);
            var selectedRoles = new[] { selectedRole };
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd    = selectedRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded) return removeResult;
            }

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded) return addResult;
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// ユーザーを無効化する（初期 Admin ユーザーは無効化不可）
        /// </summary>
        public async Task<IdentityResult> DisableUserAsync(string id)
        {
            // ポイント: 初期 Admin ユーザーはシステム管理上の必須アカウントのため無効化を禁止する
            if (id == Const.SystemAdminUserId)
                return IdentityResult.Failed(new IdentityError { Description = "初期管理者ユーザーは無効化できません。" });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "ユーザーが見つかりません。" });

            user.LockoutEnabled = true;
            user.LockoutEnd = DisabledLockoutEnd;
            user.AccessFailedCount = 0;

            return await _userManager.UpdateAsync(user);
        }

        /// <summary>
        /// バリデーションエラー後の再表示用にロール一覧を補完する
        /// </summary>
        public void FillAvailableRoles(UserManagementEditViewModel model)
        {
            model.AvailableRoles = GetAvailableRoles();
            if (string.IsNullOrWhiteSpace(model.RoleName) && model.RoleNames.Count == 1)
                model.RoleName = model.RoleNames[0];
        }

        /// <summary>
        /// Member ロールのユーザーを選択肢として取得する。
        /// </summary>
        public async Task<List<SelectListItem>> GetMemberListAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync(ApplicationRoleType.Member.ToString());
            return users
                .Where(x => !IsDisabled(x))
                .OrderBy(x => x.UserName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id,
                    Text = x.UserName ?? x.Email ?? x.Id,
                })
                .ToList();
        }

        /// <summary>
        /// Admin が現在操作対象にしている Member ID を取得する。未選択の場合は先頭の Member を選ぶ。
        /// </summary>
        public async Task<string> GetSelectedMemberUserIdAsync()
        {
            var users = await GetMemberListAsync();
            if (!users.Any()) return "";

            var session = _httpContextAccessor.HttpContext?.Session;
            var selectedUserId = session?.GetString(SelectedMemberUserIdSessionKey);
            if (!string.IsNullOrWhiteSpace(selectedUserId) && users.Any(x => x.Value == selectedUserId))
                return selectedUserId;

            selectedUserId = users[0].Value ?? "";
            if (!string.IsNullOrWhiteSpace(selectedUserId))
                session?.SetString(SelectedMemberUserIdSessionKey, selectedUserId);

            return selectedUserId;
        }

        /// <summary>
        /// Admin が操作対象にする Member ID を保存する。Member 以外は保存しない。
        /// </summary>
        public async Task SetSelectedMemberUserIdAsync(string targetUserId)
        {
            var users = await GetMemberListAsync();
            if (!users.Any(x => x.Value == targetUserId)) return;

            _httpContextAccessor.HttpContext?.Session.SetString(SelectedMemberUserIdSessionKey, targetUserId);
        }

        // ポイント: ロール選択肢を名前順で返す共通処理
        private List<SelectListItem> GetAvailableRoles()
            => _roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

        public static bool IsDisabled(ApplicationUser user)
            => user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value >= DisabledLockoutEnd;
    }
}
