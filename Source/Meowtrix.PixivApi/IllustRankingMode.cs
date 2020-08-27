namespace Meowtrix.PixivApi
{
    public enum IllustRankingMode
    {
        Day,
        Week,
        Month,
        DayMale,
        DayFemale,
        WeekOriginal,
        WeekRookie,
        DayManga,
        DayR18,
        WeekR18,
        DayMaleR18,
        DayFemaleR18,
    }

    internal static class IllustRankingModeExtensions
    {
        public static string ToQueryString(this IllustRankingMode mode)
            => mode switch
            {
                IllustRankingMode.Day => "day",
                IllustRankingMode.Week => "week",
                IllustRankingMode.Month => "month",
                IllustRankingMode.DayMale => "day_male",
                IllustRankingMode.DayFemale => "day_female",
                IllustRankingMode.WeekOriginal => "week_original",
                IllustRankingMode.WeekRookie => "week_rookie",
                IllustRankingMode.DayManga => "day_manga",
                IllustRankingMode.DayR18 => "day_r18",
                IllustRankingMode.WeekR18 => "week_r18",
                IllustRankingMode.DayMaleR18 => "day_male_r18",
                IllustRankingMode.DayFemaleR18 => "day_female_r18",
                var other => other.ToString().ToLowerInvariant(),
            };
    }
}
