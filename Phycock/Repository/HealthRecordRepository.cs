using Microsoft.EntityFrameworkCore;
using Phycock.Common;
using Phycock.Entity;
using Phycock.Entity.Enums;

namespace Phycock.Repository
{
    /// <summary>
    /// 体調記録リポジトリ。
    /// </summary>
    public class HealthRecordRepository
    {
        private readonly DBContext _context;

        /// <summary>
        /// 体調記録リポジトリを初期化する。
        /// </summary>
        public HealthRecordRepository(DBContext context)
        {
            _context = context;
        }

        /// <summary>ID で体調記録を取得する（論理削除済みは除外）。</summary>
        public virtual HealthRecordEntity? SelectById(long id)
            => _context.HealthRecord
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id && !x.DelFlag);

        /// <summary>体調記録を新規登録する。</summary>
        public virtual void Insert(HealthRecordEntity entity)
        {
            entity.SetForCreate();
            _context.HealthRecord.Add(entity);
            _context.SaveChanges();
        }

        /// <summary>体調記録を更新する。</summary>
        public virtual void Update(HealthRecordEntity entity)
        {
            entity.SetForUpdate();
            _context.HealthRecord.Update(entity);
            _context.SaveChanges();
        }

        /// <summary>体調記録を論理削除する。</summary>
        public virtual void LogicalDelete(HealthRecordEntity entity)
        {
            entity.SetForLogicalDelete();
            _context.HealthRecord.Update(entity);
            _context.SaveChanges();
        }

        /// <summary>指定ユーザー・指定日の体調記録を取得する。</summary>
        public virtual List<HealthRecordEntity> GetByUserAndDate(string userId, DateTime recordDate)
        {
            var start = recordDate.Date;
            var end = start.AddDays(1);

            return _context.HealthRecord
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.RecordDate >= start && x.RecordDate < end)
                .OrderBy(x => x.RecordTiming)
                .ThenBy(x => x.Id)
                .ToList();
        }

        /// <summary>指定ユーザー・日付・タイミングの体調記録が存在するか確認する。</summary>
        public virtual bool ExistsByUserDateTiming(string userId, DateTime recordDate, RecordTiming recordTiming, long? excludeId = null)
        {
            var start = recordDate.Date;
            var end = start.AddDays(1);

            return _context.HealthRecord
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.RecordDate >= start && x.RecordDate < end)
                .Where(x => x.RecordTiming == recordTiming)
                .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
                .Any();
        }

        /// <summary>指定ユーザー・指定期間の体調記録を取得する。</summary>
        public virtual List<HealthRecordEntity> GetByUserAndRange(string userId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            return _context.HealthRecord
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.DelFlag)
                .Where(x => x.RecordDate >= start && x.RecordDate < end)
                .OrderBy(x => x.RecordDate)
                .ThenBy(x => x.RecordTiming)
                .ThenBy(x => x.Id)
                .ToList();
        }

        /// <summary>指定期間の全ユーザーの体調記録を取得する（Admin 用）。</summary>
        public virtual List<HealthRecordEntity> GetAllByRange(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);

            return _context.HealthRecord
                .AsNoTracking()
                .Where(x => !x.DelFlag)
                .Where(x => x.RecordDate >= start && x.RecordDate < end)
                .OrderBy(x => x.RecordDate)
                .ThenBy(x => x.UserId)
                .ThenBy(x => x.RecordTiming)
                .ThenBy(x => x.Id)
                .ToList();
        }
    }
}
