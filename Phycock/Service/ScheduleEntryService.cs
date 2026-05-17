using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Common;
using Phycock.Models;
using Phycock.Repository;
using System.Reflection;

namespace Phycock.Service
{
    /// <summary>
    /// 通所予定サービス。
    /// </summary>
    public class ScheduleEntryService
    {
        private readonly ScheduleEntryRepository _repository;

        /// <summary>
        /// 通所予定サービスを初期化する。
        /// </summary>
        public ScheduleEntryService(ScheduleEntryRepository repository)
        {
            _repository = repository;
        }

        /// <summary>FullCalendar 用イベントを取得する。</summary>
        public List<ScheduleEntryJsonDto> GetEventsForCalendar(string userId, DateOnly startDate, DateOnly endDate)
        {
            return _repository.GetByUserAndRange(userId, startDate, endDate)
                .Select(ToJsonDto)
                .ToList();
        }

        /// <summary>編集対象の通所予定を取得する。所有者でも Admin でもない場合は null。</summary>
        public ScheduleEntryFormViewModel? GetForEdit(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return null;

            return ToFormViewModel(entity);
        }

        /// <summary>通所予定詳細を取得する。閲覧権限がない場合は null。</summary>
        public ScheduleEntryDetailDto? GetDetail(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return null;

            return ToDetailDto(entity);
        }

        /// <summary>作成フォーム ViewModel を生成する。</summary>
        public ScheduleEntryFormViewModel BuildCreateForm(string currentUserId, DateOnly? date = null)
        {
            return new ScheduleEntryFormViewModel
            {
                UserId = currentUserId,
                Date = date ?? DateOnly.FromDateTime(DateTime.Today),
                Session = ScheduleSession.AM,
                ActivityType = ActivityType.Program,
                ProgramType = ProgramType.SelfWork,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),
            };
        }

        /// <summary>通所予定を新規登録する。</summary>
        public void Create(ScheduleEntryFormViewModel model, string currentUserId, bool isAdmin = false)
        {
            var entity = ToEntity(model);
            entity.UserId = isAdmin && !string.IsNullOrWhiteSpace(model.UserId) ? model.UserId : currentUserId;
            _repository.Insert(entity);
        }

        /// <summary>通所予定を更新する。所有者でも Admin でもない場合は false。</summary>
        public bool Update(ScheduleEntryFormViewModel model, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(model.Id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return false;

            entity.Date = model.Date;
            entity.Session = model.Session;
            entity.IsAtHome = model.IsAtHome;
            entity.Status = model.Status;
            entity.ActivityType = model.ActivityType;
            entity.ProgramType = model.ActivityType == ActivityType.Program ? model.ProgramType : null;
            entity.ActivityNote = model.ActivityNote;
            entity.StartTime = model.StartTime;
            entity.EndTime = model.EndTime;
            entity.Notes = model.Notes;

            _repository.Update(entity);
            return true;
        }

        /// <summary>通所予定を論理削除する。所有者でも Admin でもない場合は false。</summary>
        public bool Delete(long id, string currentUserId, bool isAdmin)
        {
            var entity = _repository.SelectById(id);
            if (entity == null || (!isAdmin && entity.UserId != currentUserId)) return false;

            _repository.LogicalDelete(entity);
            return true;
        }

        /// <summary>今日の通所予定を取得する。</summary>
        public List<ScheduleEntryDetailDto> GetTodayEntries(string userId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return _repository.GetByUserAndDate(userId, today)
                .Select(ToDetailDto)
                .ToList();
        }

        private static ScheduleEntryEntity ToEntity(ScheduleEntryFormViewModel model)
        {
            return new ScheduleEntryEntity
            {
                UserId = model.UserId,
                Date = model.Date,
                Session = model.Session,
                IsAtHome = model.IsAtHome,
                Status = model.Status,
                ActivityType = model.ActivityType,
                ProgramType = model.ActivityType == ActivityType.Program ? model.ProgramType : null,
                ActivityNote = model.ActivityNote,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Notes = model.Notes,
            };
        }

        private static ScheduleEntryFormViewModel ToFormViewModel(ScheduleEntryEntity entity)
        {
            return new ScheduleEntryFormViewModel
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Date = entity.Date,
                Session = entity.Session,
                IsAtHome = entity.IsAtHome,
                Status = entity.Status,
                ActivityType = entity.ActivityType,
                ProgramType = entity.ProgramType,
                ActivityNote = entity.ActivityNote,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                Notes = entity.Notes,
            };
        }

        private static ScheduleEntryJsonDto ToJsonDto(ScheduleEntryEntity entity)
        {
            var start = CombineDateAndTime(entity.Date, entity.StartTime);
            var end = CombineDateAndTime(entity.Date, entity.EndTime);
            var color = GetColor(entity.ActivityType, entity.ProgramType, entity.IsAtHome);
            var borderColor = GetStatusBorderColor(entity.Status);
            var timeRange = BuildTimeRange(entity.StartTime, entity.EndTime);
            var place = entity.IsAtHome ? "在宅" : "通所";

            return new ScheduleEntryJsonDto
            {
                Id = entity.Id.ToString(),
                Title = BuildTitle(entity),
                Start = start?.ToString("yyyy-MM-ddTHH:mm:ss") ?? entity.Date.ToString("yyyy-MM-dd"),
                End = end?.ToString("yyyy-MM-ddTHH:mm:ss"),
                Color = color.BackgroundColor,
                BackgroundColor = color.BackgroundColor,
                BorderColor = borderColor,
                TextColor = color.TextColor,
                ExtendedProps = new ScheduleEntryExtendedProps
                {
                    PrimaryText = $"{GetSessionShortLabel(entity.Session)} {entity.ActivityType.GetDisplayName()}",
                    SecondaryText = string.Join(" / ", new[] { timeRange, place, entity.Status.GetDisplayName() }.Where(x => !string.IsNullOrWhiteSpace(x))),
                    NoteText = entity.ActivityNote,
                    Session = GetSessionShortLabel(entity.Session),
                    IsAtHome = entity.IsAtHome,
                    Status = entity.Status.GetDisplayName(),
                    ActivityType = entity.ActivityType.GetDisplayName(),
                    ActivityNote = entity.ActivityNote,
                    SortOrder = entity.StartTime.HasValue
                        ? entity.StartTime.Value.Hour * 60 + entity.StartTime.Value.Minute
                        : 540,
                },
            };
        }

        private static ScheduleEntryDetailDto ToDetailDto(ScheduleEntryEntity entity)
        {
            return new ScheduleEntryDetailDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Date = entity.Date.ToString("yyyy/MM/dd"),
                Session = entity.Session,
                IsAtHome = entity.IsAtHome,
                Status = entity.Status,
                ActivityType = entity.ActivityType,
                ProgramType = entity.ProgramType,
                ActivityNote = entity.ActivityNote,
                StartTime = entity.StartTime?.ToString("HH:mm"),
                EndTime = entity.EndTime?.ToString("HH:mm"),
                Notes = entity.Notes,
            };
        }

        private static DateTime? CombineDateAndTime(DateOnly date, TimeOnly? time)
        {
            return time.HasValue ? date.ToDateTime(time.Value) : null;
        }

        private static string BuildTitle(ScheduleEntryEntity entity)
        {
            var session = GetSessionShortLabel(entity.Session);
            return string.IsNullOrEmpty(entity.ActivityNote)
                ? session
                : $"{session}\n{entity.ActivityNote}";
        }

        private static string GetSessionShortLabel(ScheduleSession session) => session switch
        {
            ScheduleSession.AM => "AM",
            ScheduleSession.PM => "PM",
            ScheduleSession.AllDay => "終日",
            _ => session.GetDisplayName()
        };

        private static ScheduleEntryColor GetColor(ActivityType activityType, ProgramType? programType, bool isAtHome)
        {
            if (isAtHome)
                return new("#DDEFEF", "#2C9A9A", "#134F4F");

            if (activityType != ActivityType.Program)
                return GetCalendarColor(activityType);

            return programType.HasValue
                ? GetCalendarColor(programType.Value)
                : DefaultColor();
        }

        private static string GetStatusBorderColor(ScheduleStatus status)
        {
            return GetCalendarColor(status).BorderColor;
        }

        private static string BuildTimeRange(TimeOnly? startTime, TimeOnly? endTime)
        {
            if (!startTime.HasValue && !endTime.HasValue) return "";
            if (!endTime.HasValue) return $"{startTime:HH:mm}-";
            if (!startTime.HasValue) return $"-{endTime:HH:mm}";
            return $"{startTime:HH:mm}-{endTime:HH:mm}";
        }

        private static ScheduleEntryColor GetCalendarColor(Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var color = member?.GetCustomAttribute<CalendarColorAttribute>();
            return color == null
                ? DefaultColor()
                : new(color.BackgroundColor, color.BorderColor, color.TextColor);
        }

        private static ScheduleEntryColor DefaultColor()
            => new("#E7E6E6", "#A6A6A6", "#3C3C3C");

        private sealed record ScheduleEntryColor(string BackgroundColor, string BorderColor, string TextColor);
    }
}
