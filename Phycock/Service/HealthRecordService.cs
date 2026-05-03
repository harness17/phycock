using Phycock.Common;
using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Repository;

namespace Phycock.Service
{
    /// <summary>
    /// 体調記録サービス。
    /// </summary>
    public class HealthRecordService
    {
        private readonly HealthRecordRepository _repository;

        /// <summary>
        /// 体調記録サービスを初期化する。
        /// </summary>
        public HealthRecordService(HealthRecordRepository repository)
        {
            _repository = repository;
        }

        /// <summary>指定日の体調記録一覧を取得する。</summary>
        public List<HealthRecordListViewModel> GetList(string userId, DateTime? filterDate)
        {
            var date = filterDate ?? DateTime.Today;
            return _repository.GetByUserAndDate(userId, date)
                .Select(ToListViewModel)
                .ToList();
        }

        /// <summary>Admin 用に指定期間の全ユーザー体調記録一覧を取得する。</summary>
        public List<HealthRecordListViewModel> GetAllList(DateTime startDate, DateTime endDate)
        {
            return _repository.GetAllByRange(startDate, endDate)
                .Select(ToListViewModel)
                .ToList();
        }

        /// <summary>編集対象の体調記録を取得する。所有者でも Admin でもない場合は null。</summary>
        public HealthRecordFormViewModel? GetForEdit(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return null;

            return ToFormViewModel(entity);
        }

        /// <summary>JSON 詳細 DTO を取得する。閲覧権限がない場合は null。</summary>
        public HealthRecordJsonDto? GetDetail(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return null;

            return new HealthRecordJsonDto
            {
                Id = entity.Id,
                RecordDate = entity.RecordDate.ToString("yyyy/MM/dd"),
                RecordTiming = entity.RecordTiming.GetDisplayName(),
                Symptoms = FromFlags(entity.SymptomFlags).Select(x => x.GetDisplayName()).ToList(),
                Condition = entity.Condition.GetDisplayName(),
                Feeling = entity.Feeling.GetDisplayName(),
                Memo = entity.Memo,
            };
        }

        /// <summary>作成フォーム ViewModel を生成する。</summary>
        public HealthRecordFormViewModel BuildCreateForm(string currentUserId, DateTime? recordDate = null)
        {
            return new HealthRecordFormViewModel
            {
                UserId = currentUserId,
                RecordDate = recordDate ?? DateTime.Today,
            };
        }

        /// <summary>体調記録を新規登録する。</summary>
        public void Create(HealthRecordFormViewModel model, string currentUserId, bool isAdmin = false)
        {
            var entity = new HealthRecordEntity
            {
                UserId = isAdmin && !string.IsNullOrWhiteSpace(model.UserId) ? model.UserId : currentUserId,
                RecordDate = model.RecordDate.Date,
                RecordTiming = model.RecordTiming,
                SymptomFlags = ToFlags(model.SelectedSymptoms),
                Condition = model.Condition,
                Feeling = model.Feeling,
                Memo = model.Memo,
            };

            _repository.Insert(entity);
        }

        /// <summary>体調記録を更新する。所有者でも Admin でもない場合は false。</summary>
        public bool Update(HealthRecordFormViewModel model, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(model.Id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return false;

            entity.RecordDate = model.RecordDate.Date;
            entity.RecordTiming = model.RecordTiming;
            entity.SymptomFlags = ToFlags(model.SelectedSymptoms);
            entity.Condition = model.Condition;
            entity.Feeling = model.Feeling;
            entity.Memo = model.Memo;

            _repository.Update(entity);
            return true;
        }

        /// <summary>体調記録を論理削除する。所有者でも Admin でもない場合は false。</summary>
        public bool Delete(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return false;

            _repository.LogicalDelete(entity);
            return true;
        }

        /// <summary>今日の体調サマリーを取得する。</summary>
        public List<HealthRecordListViewModel> GetTodaySummary(string userId)
        {
            return _repository.GetByUserAndDate(userId, DateTime.Today)
                .Select(ToListViewModel)
                .ToList();
        }

        /// <summary>直近7日分の体調平均を取得する。</summary>
        public WeeklySummaryDto GetWeeklySummary(string userId, DateTime? endDate = null)
        {
            var end = (endDate ?? DateTime.Today).Date;
            var start = end.AddDays(-6);
            var records = _repository.GetByUserAndRange(userId, start, end);

            return new WeeklySummaryDto
            {
                StartDate = start,
                EndDate = end,
                AverageCondition = records.Count == 0 ? null : records.Average(x => (int)x.Condition),
                AverageFeeling = records.Count == 0 ? null : records.Average(x => (int)x.Feeling),
            };
        }

        /// <summary>バリデーション再表示用の補完処理。</summary>
        public HealthRecordFormViewModel FillSelections(HealthRecordFormViewModel model)
        {
            return model;
        }

        private static HealthRecordListViewModel ToListViewModel(HealthRecordEntity entity)
        {
            return new HealthRecordListViewModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                RecordDate = entity.RecordDate,
                RecordTiming = entity.RecordTiming,
                Symptoms = string.Join("、", FromFlags(entity.SymptomFlags).Select(x => x.GetDisplayName())),
                SymptomFlags = entity.SymptomFlags,
                Condition = entity.Condition,
                Feeling = entity.Feeling,
                Memo = entity.Memo,
            };
        }

        private HealthRecordFormViewModel ToFormViewModel(HealthRecordEntity entity)
        {
            return FillSelections(new HealthRecordFormViewModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                RecordDate = entity.RecordDate,
                RecordTiming = entity.RecordTiming,
                SelectedSymptoms = FromFlags(entity.SymptomFlags),
                Condition = entity.Condition,
                Feeling = entity.Feeling,
                Memo = entity.Memo,
            });
        }

        public static long ToFlags(IEnumerable<SymptomType> symptoms)
        {
            return symptoms
                .Where(x => x != SymptomType.None)
                .Aggregate(0L, (acc, symptom) => acc | (long)symptom);
        }

        public static List<SymptomType> FromFlags(long flags)
        {
            return Enum.GetValues<SymptomType>()
                .Where(x => x != SymptomType.None && (flags & (long)x) != 0)
                .ToList();
        }
    }
}
