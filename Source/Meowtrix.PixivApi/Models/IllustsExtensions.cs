using System.Collections.Generic;
using System.Linq;

namespace Meowtrix.PixivApi.Models
{
    public static class IllustsExtensions
    {
        public static IAsyncEnumerable<Illust> R18(this IAsyncEnumerable<Illust> source)
            => source.Where(x => x.IsR18);

        public static IAsyncEnumerable<Illust> AllAge(this IAsyncEnumerable<Illust> source)
            => source.Where(x => !x.IsR18);

        public static IAsyncEnumerable<Illust> Age(this IAsyncEnumerable<Illust> source, AgeRestriction age)
            => age switch
            {
                AgeRestriction.AllAge => source.AllAge(),
                AgeRestriction.R18 => source.R18(),
                _ => source
            };

        public static IAsyncEnumerable<Illust> WithTag(this IAsyncEnumerable<Illust> source, string tag)
            => source.Where(x => x.Tags.Any(t => t.Name == tag));

        public static IAsyncEnumerable<Illust> WithoutTag(this IAsyncEnumerable<Illust> source, string tag)
            => source.Where(x => !x.Tags.Any(t => t.Name == tag));
    }
}
