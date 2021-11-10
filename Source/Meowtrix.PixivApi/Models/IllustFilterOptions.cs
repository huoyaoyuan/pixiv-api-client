using System;

namespace Meowtrix.PixivApi.Models
{
    public record IllustFilterOptions
    {
        public IllustSortMode SortMode { get; init; } = IllustSortMode.Latest;
        public int? MaxBookmarkCount { get; init; }
        public int? MinBookmarkCount { get; init; }

#if NET6_0_OR_GREATER
        public DateOnly? StartDateOnly { get; init; }
        public DateOnly? EndDateOnly { get; init; }

        public DateTime? StartDate
        {
            get => StartDateOnly?.ToDateTime(default, DateTimeKind.Unspecified);
            init => StartDateOnly = value is DateTime d ? DateOnly.FromDateTime(d) : null;
        }

        public DateTime? EndDate
        {
            get => EndDateOnly?.ToDateTime(default, DateTimeKind.Unspecified);
            init => EndDateOnly = value is DateTime d ? DateOnly.FromDateTime(d) : null;
        }
#else
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
#endif
    }
}
