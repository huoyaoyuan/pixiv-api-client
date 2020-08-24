using System;
using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class UserBookmarkTags
    {
        public ImmutableArray<object> BookmarkTags { get; set; }
        public Uri NextUrl { get; set; }
    }
}
