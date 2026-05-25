using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Dev.CommonLibrary.Entity;
using Phycock.Entity;

namespace Phycock.Common
{
    /// <summary>
    /// DBコンテクスト
    /// </summary>
    public class DBContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // テーブル名のカスタマイズ
            modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUser");
            modelBuilder.Entity<ApplicationRole>().ToTable("ApplicationRole");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("ApplicationUserRole");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("ApplicationUserClaim");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("ApplicationUserLogin");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("ApplicationRoleClaim");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("ApplicationUserToken");

            // DateTime型の設定
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetColumnType("datetime2");
            }

            modelBuilder.Entity<HealthRecordEntity>()
                .HasIndex(x => new { x.UserId, x.RecordDate, x.RecordTiming, x.RecordTime })
                .IsUnique()
                .HasFilter("[DelFlag] = 0");

            modelBuilder.Entity<PeriodReflectionEntity>()
                .HasIndex(x => new { x.UserId, x.PeriodType, x.PeriodStart })
                .IsUnique()
                .HasFilter("[DelFlag] = 0");
        }

        #region DbSet

        public DbSet<UserPreviousPassword> UserPreviousPasswords { get; set; }

        // 通知
        public DbSet<NotificationEntity> Notification { get; set; }

        // 体調記録
        public DbSet<HealthRecordEntity> HealthRecord { get; set; }

        // 睡眠記録
        public DbSet<SleepRecordEntity> SleepRecord { get; set; }

        // 通所予定
        public DbSet<ScheduleEntryEntity> ScheduleEntry { get; set; }

        // 期間所感（週次／月次レポートの自己所感）
        public DbSet<PeriodReflectionEntity> PeriodReflection { get; set; }

        #endregion
    }
}
