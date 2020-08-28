using System;

namespace Meowtrix.PixivApi
{
    public enum IllustSortMode
    {
        Latest,
        Oldest,
        Popular,
        PopularWithMale,
        PopularWithFemale,
    }

    internal static class IllustSortModeExtensions
    {
        public static string ToQueryString(this IllustSortMode sort)
            => sort switch
            {
                IllustSortMode.Latest => "date_desc",
                IllustSortMode.Oldest => "date_asc",
                IllustSortMode.Popular => "popular_desc",
                IllustSortMode.PopularWithMale => "popular_male_desc",
                IllustSortMode.PopularWithFemale => "popular_female_desc",
                _ => throw new ArgumentException("Unknown sort mode", nameof(sort))
            };
    }
}
