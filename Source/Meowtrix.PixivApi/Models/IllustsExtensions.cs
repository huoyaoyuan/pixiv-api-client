using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Meowtrix.PixivApi.Models
{
    public static class IllustsExtensions
    {
        public static async IAsyncEnumerable<Illust> R18(this IAsyncEnumerable<Illust> source)
        {
            await foreach (var i in source.ConfigureAwait(false))
            {
                if (i.IsR18)
                    yield return i;
            }
        }

        public static async IAsyncEnumerable<Illust> AllAge(this IAsyncEnumerable<Illust> source)
        {
            await foreach (var i in source.ConfigureAwait(false))
            {
                if (!i.IsR18)
                    yield return i;
            }
        }

        public static IAsyncEnumerable<Illust> Age(this IAsyncEnumerable<Illust> source, AgeRestriction age)
            => age switch
            {
                AgeRestriction.AllAge => source.AllAge(),
                AgeRestriction.R18 => source.R18(),
                _ => source
            };

        public static async IAsyncEnumerable<Illust> WithTag(this IAsyncEnumerable<Illust> source, string tag)
        {
            await foreach (var i in source.ConfigureAwait(false))
            {
                if (i.Tags.Contains(tag))
                    yield return i;
            }
        }

        public static async IAsyncEnumerable<Illust> WithoutTag(this IAsyncEnumerable<Illust> source, string tag)
        {
            await foreach (var i in source.ConfigureAwait(false))
            {
                if (!i.Tags.Contains(tag))
                    yield return i;
            }
        }
    }
}
