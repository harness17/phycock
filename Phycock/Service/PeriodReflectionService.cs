using Phycock.Entity;
using Phycock.Entity.Enums;
using Phycock.Models;
using Phycock.Repository;

namespace Phycock.Service
{
    /// <summary>
    /// 期間所感サービス。
    /// </summary>
    public class PeriodReflectionService
    {
        private readonly PeriodReflectionRepository _repository;

        public PeriodReflectionService(PeriodReflectionRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 指定ユーザー・期間の所感を取得する。未登録の場合は空の ViewModel を返す。
        /// </summary>
        public PeriodReflectionViewModel GetOrEmpty(string userId, PeriodType periodType, DateTime periodStart)
        {
            var normalized = NormalizePeriodStart(periodType, periodStart);
            var entity = _repository.SelectByPeriod(userId, periodType, normalized);
            if (entity == null)
            {
                return new PeriodReflectionViewModel
                {
                    UserId = userId,
                    PeriodType = periodType,
                    PeriodStart = normalized
                };
            }
            return ToViewModel(entity);
        }

        /// <summary>
        /// 所感を保存する（無ければ新規、有れば更新）。全項目空の場合は何もしない。
        /// </summary>
        public void Save(string targetUserId, PeriodReflectionViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(targetUserId)) return;

            var normalized = NormalizePeriodStart(vm.PeriodType, vm.PeriodStart);
            var existing = _repository.SelectByPeriod(targetUserId, vm.PeriodType, normalized);

            if (existing == null)
            {
                if (vm.IsEmpty) return; // 空登録は無視
                _repository.Insert(new PeriodReflectionEntity
                {
                    UserId = targetUserId,
                    PeriodType = vm.PeriodType,
                    PeriodStart = normalized,
                    SelfEvaluation = TrimNullable(vm.SelfEvaluation),
                    Burden = TrimNullable(vm.Burden),
                    Improvement = TrimNullable(vm.Improvement),
                    Appetite = TrimNullable(vm.Appetite),
                    Sleep = TrimNullable(vm.Sleep)
                });
            }
            else
            {
                existing.SelfEvaluation = TrimNullable(vm.SelfEvaluation);
                existing.Burden = TrimNullable(vm.Burden);
                existing.Improvement = TrimNullable(vm.Improvement);
                existing.Appetite = TrimNullable(vm.Appetite);
                existing.Sleep = TrimNullable(vm.Sleep);
                _repository.Update(existing);
            }
        }

        /// <summary>週次は直近日曜の 00:00、月次は月初日 00:00 に正規化する。</summary>
        public static DateTime NormalizePeriodStart(PeriodType type, DateTime input)
        {
            var d = input.Date;
            return type switch
            {
                PeriodType.Weekly => d.AddDays(-(int)d.DayOfWeek),
                PeriodType.Monthly => new DateTime(d.Year, d.Month, 1),
                _ => d
            };
        }

        private static string? TrimNullable(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim();
        }

        private static PeriodReflectionViewModel ToViewModel(PeriodReflectionEntity e) => new()
        {
            Id = e.Id,
            UserId = e.UserId,
            PeriodType = e.PeriodType,
            PeriodStart = e.PeriodStart,
            SelfEvaluation = e.SelfEvaluation,
            Burden = e.Burden,
            Improvement = e.Improvement,
            Appetite = e.Appetite,
            Sleep = e.Sleep
        };
    }
}
