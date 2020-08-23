using System;
using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class RecommendedIllusts
    {
        public ImmutableArray<UserIllustPreview> Illusts { get; set; }
        public ImmutableArray<object> RankingIllusts { get; set; }
        public bool ContestExists { get; set; }
        public object PrivacyPolicy { get; set; }
        public Uri NextUrl { get; set; }
    }
}
