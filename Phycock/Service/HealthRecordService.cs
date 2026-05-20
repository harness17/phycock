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

        /// <summary>FullCalendar 用イベントを取得する。</summary>
        public List<HealthRecordCalendarEventDto> GetEventsForCalendar(string userId, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(userId) || endDate <= startDate) return new List<HealthRecordCalendarEventDto>();

            return _repository.GetByUserAndRange(userId, startDate, endDate.AddDays(-1))
                .Select(ToCalendarEventDto)
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
                RecordTiming = FormatTiming(entity),
                Symptoms = FromFlags(entity.SymptomFlags).Select(x => x.GetDisplayName()).ToList(),
                Condition = entity.Condition.GetDisplayName(),
                Feeling = entity.Feeling.GetDisplayName(),
                Memo = entity.Memo,
            };
        }

        /// <summary>作成フォーム ViewModel を生成する。</summary>
        public HealthRecordFormViewModel BuildCreateForm(string currentUserId, DateTime? recordDate = null)
        {
            var form = new HealthRecordFormViewModel
            {
                UserId = currentUserId,
                RecordDate = recordDate ?? DateTime.Today,
                RecordTime = GetDefaultCustomTime(),
            };

            return FillSelections(form);
        }

        /// <summary>体調記録を新規登録する。</summary>
        public bool Create(HealthRecordFormViewModel model, string currentUserId, bool isAdmin = false)
        {
            var targetUserId = isAdmin && !string.IsNullOrWhiteSpace(model.UserId) ? model.UserId : currentUserId;
            var recordTime = NormalizeRecordTime(model.RecordTiming, model.RecordTime);
            if (IsDuplicate(targetUserId, model.RecordDate, model.RecordTiming, recordTime)) return false;

            var entity = new HealthRecordEntity
            {
                UserId = targetUserId,
                RecordDate = model.RecordDate.Date,
                RecordTiming = model.RecordTiming,
                RecordTime = recordTime,
                SymptomFlags = ToFlags(model.SelectedSymptoms),
                Condition = model.Condition,
                Feeling = model.Feeling,
                Memo = model.Memo,
            };

            _repository.Insert(entity);
            return true;
        }

        /// <summary>体調記録を更新する。所有者でも Admin でもない場合は false。</summary>
        public bool Update(HealthRecordFormViewModel model, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(model.Id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return false;
            var recordTime = NormalizeRecordTime(model.RecordTiming, model.RecordTime);
            if (IsDuplicate(entity.UserId, model.RecordDate, model.RecordTiming, recordTime, model.Id)) return false;

            entity.RecordDate = model.RecordDate.Date;
            entity.RecordTiming = model.RecordTiming;
            entity.RecordTime = recordTime;
            entity.SymptomFlags = ToFlags(model.SelectedSymptoms);
            entity.Condition = model.Condition;
            entity.Feeling = model.Feeling;
            entity.Memo = model.Memo;

            _repository.Update(entity);
            return true;
        }

        /// <summary>同一ユーザー・同一日・同一タイミングの記録が存在するか確認する。</summary>
        public bool IsDuplicate(string userId, DateTime recordDate, RecordTiming recordTiming, TimeOnly? recordTime = null, long? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;

            return _repository.ExistsByUserDateTiming(userId, recordDate, recordTiming, NormalizeRecordTime(recordTiming, recordTime), excludeId);
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
            model.DisabledRecordTimings = GetDisabledRecordTimings(model.UserId, model.RecordDate, model.Id == 0 ? null : model.Id);
            if (model.Id == 0 && model.DisabledRecordTimings.Contains(model.RecordTiming))
            {
                var firstSelectable = Enum.GetValues<RecordTiming>()
                    .FirstOrDefault(x => !model.DisabledRecordTimings.Contains(x));
                model.RecordTiming = firstSelectable;
            }

            return model;
        }

        /// <summary>指定日の登録済みタイミング一覧を取得する。</summary>
        public List<RecordTiming> GetDisabledRecordTimings(string userId, DateTime recordDate, long? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<RecordTiming>();

            return (_repository.GetByUserAndDate(userId, recordDate) ?? new List<HealthRecordEntity>())
                .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
                .Where(x => x.RecordTiming != RecordTiming.Custom)
                .Select(x => x.RecordTiming)
                .Distinct()
                .ToList();
        }

        private static HealthRecordListViewModel ToListViewModel(HealthRecordEntity entity)
        {
            return new HealthRecordListViewModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                RecordDate = entity.RecordDate,
                RecordTiming = entity.RecordTiming,
                RecordTime = entity.RecordTime,
                TimingLabel = FormatTiming(entity),
                Symptoms = string.Join("、", FromFlags(entity.SymptomFlags).Select(x => x.GetDisplayName())),
                SymptomFlags = entity.SymptomFlags,
                Condition = entity.Condition,
                Feeling = entity.Feeling,
                Memo = entity.Memo,
            };
        }

        private static HealthRecordCalendarEventDto ToCalendarEventDto(HealthRecordEntity entity)
        {
            var color = GetConditionColor(entity.Condition);
            var timingText = FormatTiming(entity);
            var primaryText = $"{timingText} 体調:{entity.Condition.GetDisplayName()}";
            var symptoms = string.Join("、", FromFlags(entity.SymptomFlags).Select(x => x.GetDisplayName()));
            var start = entity.RecordTiming == RecordTiming.Custom && entity.RecordTime.HasValue
                ? $"{entity.RecordDate:yyyy-MM-dd}T{entity.RecordTime.Value:HH\\:mm\\:ss}"
                : entity.RecordDate.ToString("yyyy-MM-dd");

            return new HealthRecordCalendarEventDto
            {
                Id = entity.Id.ToString(),
                Title = primaryText,
                Start = start,
                AllDay = entity.RecordTiming != RecordTiming.Custom,
                Color = color.BackgroundColor,
                BackgroundColor = color.BackgroundColor,
                BorderColor = color.BorderColor,
                TextColor = color.TextColor,
                ExtendedProps = new CalendarEventExtendedProps
                {
                    PrimaryText = primaryText,
                    SecondaryText = $"気分:{entity.Feeling.GetDisplayName()}",
                    NoteText = string.IsNullOrWhiteSpace(symptoms) ? null : symptoms,
                    SortOrder = GetTimingSortOrder(entity),
                },
            };
        }

        /// <summary>統合カレンダーの並び順キー。1日の流れ上の代表時刻（分換算）を返す。</summary>
        internal static int GetTimingSortOrder(HealthRecordEntity entity)
        {
            if (entity.RecordTiming == RecordTiming.Custom && entity.RecordTime.HasValue)
            {
                return entity.RecordTime.Value.Hour * 60 + entity.RecordTime.Value.Minute;
            }

            return GetTimingSortOrder(entity.RecordTiming);
        }

        private static int GetTimingSortOrder(RecordTiming timing)
        {
            return timing switch
            {
                RecordTiming.Morning => 360,   // 06:00（本睡眠の後）
                RecordTiming.Noon => 510,      // 08:30（通所予定の前）
                RecordTiming.Evening => 945,   // 15:45（通所予定の後）
                RecordTiming.Night => 1439,    // 23:59（1日の最後）
                _ => 720,
            };
        }

        /// <summary>
        /// 体調レベルの表示色を返す。
        /// 睡眠記録・通所スケジュールと同じ「淡いパステル背景＋彩度のあるボーダー＋濃い文字色」方針。
        /// </summary>
        private static HealthRecordColor GetConditionColor(ConditionLevel condition)
        {
            return condition switch
            {
                ConditionLevel.VeryGood => new("#D1E7DD", "#198754", "#0F5132"),
                ConditionLevel.Good => new("#D2F4EA", "#20C997", "#0B4F3A"),
                ConditionLevel.Normal => new("#D8EAF7", "#2874A6", "#1B4F72"),
                ConditionLevel.Bad => new("#FCE4D6", "#FD7E14", "#7A3E0A"),
                ConditionLevel.VeryBad => new("#F8D7DA", "#DC3545", "#842029"),
                _ => new("#E9ECEF", "#6C757D", "#343A40"),
            };
        }

        private sealed record HealthRecordColor(string BackgroundColor, string BorderColor, string TextColor);

        private HealthRecordFormViewModel ToFormViewModel(HealthRecordEntity entity)
        {
            return FillSelections(new HealthRecordFormViewModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                RecordDate = entity.RecordDate,
                RecordTiming = entity.RecordTiming,
                RecordTime = entity.RecordTime ?? GetDefaultCustomTime(),
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

        private static TimeOnly? NormalizeRecordTime(RecordTiming timing, TimeOnly? recordTime)
            => timing == RecordTiming.Custom ? recordTime ?? GetDefaultCustomTime() : null;

        private static TimeOnly GetDefaultCustomTime()
        {
            var now = DateTime.Now;
            return new TimeOnly(now.Hour, now.Minute);
        }

        internal static string FormatTiming(HealthRecordEntity entity)
            => entity.RecordTiming == RecordTiming.Custom && entity.RecordTime.HasValue
                ? $"{entity.RecordTime.Value:HH\\:mm}"
                : entity.RecordTiming.GetDisplayName();
    }
}
