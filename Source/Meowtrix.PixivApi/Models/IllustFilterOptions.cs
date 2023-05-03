using System;

namespace Meowtrix.PixivApi.Models
{
    public record IllustFilterOptions
    {
        public IllustSortMode SortMode { get; init; } = IllustSortMode.Latest;
        public int? MaxBookmarkCount { get; init; }
        public int? MinBookmarkCount { get; init; }

        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
    }
}
