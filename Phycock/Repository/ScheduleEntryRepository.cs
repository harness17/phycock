using Microsoft.EntityFrameworkCore;
using Phycock.Common;
using Phycock.Entity;

namespace Phycock.Repository
{
    /// <summary>
    /// 通所予定リポジトリ。
    /// </summary>
    public class ScheduleEntryRepository
    {
        private readonly DBContext _context;

        /// <summary>
        /// 通所予定リポジトリを初期化する。
        /// </summary>
        public ScheduleEntryRepository(DBContext context)
        {
            _context = context;
        }

        /// <summary>ID で通所予定を取得する（論理削除済みは除外）。</summary>
        public virtual ScheduleEntryEntity? SelectById(long id)
            => _context.ScheduleEntry
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id && !x.DelFlag);

        /// <summary>通所予定を新規登録する。</summary>
        public virtual void Insert(ScheduleEntryEntity entity)
        {
            entity.SetForCreate();
            _context.ScheduleEntry.Add(entity);
            _context.SaveChanges();
        }

        /// <summary>通所予定を更新する。</summary>
        public virtual void Update(ScheduleEntryEntity entity)
        {
            entity.SetForUpdate();
            _context.ScheduleEntry.Update(entity);
            _context.SaveChanges();
        }

        /// <summary>通所予定を論理削除する。</summary>
        public virtual void LogicalDelete(ScheduleEntryEntity entity)
        {
            entity.SetForLogicalDelete();
            _context.ScheduleEntry.Update(entity);
            _context.SaveChanges();
        }

        /// <summary>指定ユーザー・指定日の通所予定を取得する。</summary>
        public virtual List<ScheduleEntryEntity> GetByUserAndDate(string userId, DateOnly date)
        {
            return _context.ScheduleEntry
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.Date == date)
                .OrderBy(x => x.Session)
                .ThenBy(x => x.StartTime)
                .ThenBy(x => x.Id)
                .ToList();
        }

        /// <summary>指定ユーザー・指定月の通所予定を取得する。</summary>
        public virtual List<ScheduleEntryEntity> GetByUserAndMonth(string userId, int year, int month)
        {
            var start = new DateOnly(year, month, 1);
            var end = start.AddMonths(1);

            return _context.ScheduleEntry
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.Date >= start && x.Date < end)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Session)
                .ThenBy(x => x.StartTime)
                .ThenBy(x => x.Id)
                .ToList();
        }

        /// <summary>指定ユーザー・指定期間の通所予定を取得する。</summary>
        public virtual List<ScheduleEntryEntity> GetByUserAndRange(string userId, DateOnly startDate, DateOnly endDate)
        {
            return _context.ScheduleEntry
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.Date >= startDate && x.Date <= endDate)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Session)
                .ThenBy(x => x.StartTime)
                .ThenBy(x => x.Id)
                .ToList();
        }
    }
}
