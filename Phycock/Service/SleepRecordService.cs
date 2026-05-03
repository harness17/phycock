using Phycock.Entity;
using Phycock.Models;
using Phycock.Repository;

namespace Phycock.Service
{
    /// <summary>
    /// 睡眠記録サービス。
    /// </summary>
    public class SleepRecordService
    {
        private readonly SleepRecordRepository _repository;

        /// <summary>
        /// 睡眠記録サービスを初期化する。
        /// </summary>
        public SleepRecordService(SleepRecordRepository repository)
        {
            _repository = repository;
        }

        /// <summary>指定日の睡眠記録一覧を取得する。</summary>
        public List<SleepRecordListViewModel> GetList(string userId, DateTime? filterDate)
        {
            var date = filterDate ?? DateTime.Today;
            return _repository.GetByUserAndDate(userId, date)
                .Select(ToListViewModel)
                .ToList();
        }

        /// <summary>指定期間の睡眠記録一覧を取得する。</summary>
        public List<SleepRecordListViewModel> GetByRange(string userId, DateTime startDate, DateTime endDate)
        {
            return _repository.GetByUserAndRange(userId, startDate, endDate)
                .Select(ToListViewModel)
                .ToList();
        }

        /// <summary>Admin 用に指定期間の全ユーザー睡眠記録一覧を取得する。</summary>
        public List<SleepRecordListViewModel> GetAllList(DateTime startDate, DateTime endDate)
        {
            return _repository.GetAllByRange(startDate, endDate)
                .Select(ToListViewModel)
                .ToList();
        }

        /// <summary>編集対象の睡眠記録を取得する。所有者でも Admin でもない場合は null。</summary>
        public SleepRecordFormViewModel? GetForEdit(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return null;

            return ToFormViewModel(entity);
        }

        /// <summary>作成フォーム ViewModel を生成する。</summary>
        public SleepRecordFormViewModel BuildCreateForm(string currentUserId, DateTime? recordDate = null)
        {
            return new SleepRecordFormViewModel
            {
                UserId = currentUserId,
                RecordDate = recordDate ?? DateTime.Today,
                StartTime = new TimeOnly(22, 0),
                EndTime = new TimeOnly(6, 0),
            };
        }

        /// <summary>睡眠記録を新規登録する。</summary>
        public void Create(SleepRecordFormViewModel model, string currentUserId, bool isAdmin = false)
        {
            var (startDate, endDate) = BuildSleepDateTimes(model.RecordDate, model.StartTime, model.EndTime);
            var entity = new SleepRecordEntity
            {
                UserId = isAdmin && !string.IsNullOrWhiteSpace(model.UserId) ? model.UserId : currentUserId,
                RecordDate = model.RecordDate.Date,
                StartDate = startDate,
                EndDate = endDate,
                SleepType = model.SleepType,
                Memo = model.Memo,
            };

            _repository.Insert(entity);
        }

        /// <summary>睡眠記録を更新する。所有者でも Admin でもない場合は false。</summary>
        public bool Update(SleepRecordFormViewModel model, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(model.Id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return false;

            var (startDate, endDate) = BuildSleepDateTimes(model.RecordDate, model.StartTime, model.EndTime);
            entity.RecordDate = model.RecordDate.Date;
            entity.StartDate = startDate;
            entity.EndDate = endDate;
            entity.SleepType = model.SleepType;
            entity.Memo = model.Memo;

            _repository.Update(entity);
            return true;
        }

        /// <summary>睡眠記録を論理削除する。所有者でも Admin でもない場合は false。</summary>
        public bool Delete(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return false;

            _repository.LogicalDelete(entity);
            return true;
        }

        /// <summary>指定期間の睡眠時間合計を計算する。終了日時がない記録は除外する。</summary>
        public TimeSpan GetSleepDuration(string userId, DateTime startDate, DateTime endDate)
        {
            return _repository.GetByUserAndRange(userId, startDate, endDate)
                .Where(x => x.EndDate.HasValue && x.EndDate.Value > x.StartDate)
                .Aggregate(TimeSpan.Zero, (total, record) => total + (record.EndDate!.Value - record.StartDate));
        }

        private static SleepRecordListViewModel ToListViewModel(SleepRecordEntity entity)
        {
            return new SleepRecordListViewModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                RecordDate = entity.RecordDate,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                SleepType = entity.SleepType,
                Duration = entity.EndDate.HasValue && entity.EndDate.Value > entity.StartDate
                    ? entity.EndDate.Value - entity.StartDate
                    : null,
                Memo = entity.Memo,
            };
        }

        private static SleepRecordFormViewModel ToFormViewModel(SleepRecordEntity entity)
        {
            return new SleepRecordFormViewModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                RecordDate = entity.RecordDate,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                StartTime = TimeOnly.FromDateTime(entity.StartDate),
                EndTime = entity.EndDate.HasValue ? TimeOnly.FromDateTime(entity.EndDate.Value) : null,
                SleepType = entity.SleepType,
                Memo = entity.Memo,
            };
        }

        public static (DateTime StartDate, DateTime? EndDate) BuildSleepDateTimes(
            DateTime recordDate,
            TimeOnly startTime,
            TimeOnly? endTime)
        {
            var startDate = recordDate.Date.Add(startTime.ToTimeSpan());
            if (!endTime.HasValue) return (startDate, null);

            var endDate = recordDate.Date.Add(endTime.Value.ToTimeSpan());
            if (endDate <= startDate)
                endDate = endDate.AddDays(1);

            return (startDate, endDate);
        }
    }
}
