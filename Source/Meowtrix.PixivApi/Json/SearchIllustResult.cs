using System;
using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class SearchIllustResult
    {
        public ImmutableArray<UserIllustPreview> Illusts { get; set; }
        public Uri NextUrl { get; set; }
        public int SearchSpanLimit { get; set; }
    }
}
