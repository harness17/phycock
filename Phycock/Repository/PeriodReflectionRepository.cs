using Microsoft.EntityFrameworkCore;
using Phycock.Common;
using Phycock.Entity;
using Phycock.Entity.Enums;

namespace Phycock.Repository
{
    /// <summary>
    /// 期間所感リポジトリ。
    /// </summary>
    public class PeriodReflectionRepository
    {
        private readonly DBContext _context;

        public PeriodReflectionRepository(DBContext context)
        {
            _context = context;
        }

        /// <summary>ID で取得する（論理削除済みは除外）。</summary>
        public virtual PeriodReflectionEntity? SelectById(long id)
            => _context.PeriodReflection
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id && !x.DelFlag);

        /// <summary>ユーザー・期間種別・期間開始日で1件取得する。</summary>
        public virtual PeriodReflectionEntity? SelectByPeriod(string userId, PeriodType periodType, DateTime periodStart)
            => _context.PeriodReflection
                .AsNoTracking()
                .FirstOrDefault(x => x.UserId == userId
                                     && x.PeriodType == periodType
                                     && x.PeriodStart == periodStart
                                     && !x.DelFlag);

        /// <summary>新規登録する。</summary>
        public virtual void Insert(PeriodReflectionEntity entity)
        {
            entity.SetForCreate();
            _context.PeriodReflection.Add(entity);
            _context.SaveChanges();
        }

        /// <summary>更新する。</summary>
        public virtual void Update(PeriodReflectionEntity entity)
        {
            entity.SetForUpdate();
            _context.PeriodReflection.Update(entity);
            _context.SaveChanges();
        }
    }
}
