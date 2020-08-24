using System;
using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class UserFollowList
    {
        public ImmutableArray<UserPreview> UserPreviews { get; set; }
        public Uri NextUrl { get; set; }
    }

    public class UserPreview
    {
        public IllustUser User { get; set; }
        public ImmutableArray<UserIllustPreview> Illusts { get; set; }
        public ImmutableArray<object> Novels { get; set; }
        public bool IsMuted { get; set; }
    }
}
