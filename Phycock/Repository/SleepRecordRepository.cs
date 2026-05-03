using Microsoft.EntityFrameworkCore;
using Phycock.Common;
using Phycock.Entity;

namespace Phycock.Repository
{
    /// <summary>
    /// 睡眠記録リポジトリ。
    /// </summary>
    public class SleepRecordRepository
    {
        private readonly DBContext _context;

        /// <summary>
        /// 睡眠記録リポジトリを初期化する。
        /// </summary>
        public SleepRecordRepository(DBContext context)
        {
            _context = context;
        }

        /// <summary>ID で睡眠記録を取得する（論理削除済みは除外）。</summary>
        public virtual SleepRecordEntity? SelectById(long id)
            => _context.SleepRecord
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id && !x.DelFlag);

        /// <summary>睡眠記録を新規登録する。</summary>
        public virtual void Insert(SleepRecordEntity entity)
        {
            entity.SetForCreate();
            _context.SleepRecord.Add(entity);
            _context.SaveChanges();
        }

        /// <summary>睡眠記録を更新する。</summary>
        public virtual void Update(SleepRecordEntity entity)
        {
            entity.SetForUpdate();
            _context.SleepRecord.Update(entity);
            _context.SaveChanges();
        }

        /// <summary>睡眠記録を論理削除する。</summary>
        public virtual void LogicalDelete(SleepRecordEntity entity)
        {
            entity.SetForLogicalDelete();
            _context.SleepRecord.Update(entity);
            _context.SaveChanges();
        }

        /// <summary>指定ユーザー・指定日の睡眠記録を取得する。</summary>
        public virtual List<SleepRecordEntity> GetByUserAndDate(string userId, DateTime recordDate)
        {
            var start = recordDate.Date;
            var end = start.AddDays(1);

            return _context.SleepRecord
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.RecordDate >= start && x.RecordDate < end)
                .OrderBy(x => x.StartDate)
                .ThenBy(x => x.Id)
                .ToList();
        }

        /// <summary>指定ユーザー・指定期間の睡眠記録を取得する。</summary>
        public virtual List<SleepRecordEntity> GetByUserAndRange(string userId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            return _context.SleepRecord
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.RecordDate >= start && x.RecordDate < end)
                .OrderBy(x => x.RecordDate)
                .ThenBy(x => x.StartDate)
                .ThenBy(x => x.Id)
                .ToList();
        }

        /// <summary>指定期間の全ユーザーの睡眠記録を取得する（Admin 用）。</summary>
        public virtual List<SleepRecordEntity> GetAllByRange(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            return _context.SleepRecord
                .AsNoTracking()
                .Where(x => !x.DelFlag)
                .Where(x => x.RecordDate >= start && x.RecordDate < end)
                .OrderBy(x => x.RecordDate)
                .ThenBy(x => x.UserId)
                .ThenBy(x => x.StartDate)
                .ThenBy(x => x.Id)
                .ToList();
        }
    }
}
