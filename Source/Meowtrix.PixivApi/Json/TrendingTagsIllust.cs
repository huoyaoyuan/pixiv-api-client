using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class TrendingTagsIllust
    {
        public ImmutableArray<TrendTag> TrendTags { get; set; }
    }

    public class TrendTag
    {
        public string Tag { get; set; }
        public string TranslatedName { get; set; }
        public UserIllustPreview Illust { get; set; }
    }
}
