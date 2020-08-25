using System;
using System.Collections.Immutable;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class UserIllusts
    {
        public ImmutableArray<UserIllustPreview> Illusts { get; set; }
        public Uri NextUrl { get; set; }
    }

    public class UserIllustPreview
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public SizedImageUrls ImageUrls { get; set; }
        public string Caption { get; set; }
        public int Restrict { get; set; }
        public IllustUser User { get; set; }
        public ImmutableArray<IllustTag> Tags { get; set; }
        public ImmutableArray<string> Tools { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public int PageCount { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int SanityLevel { get; set; }
        public int XRestrict { get; set; }

        public class IllustSeries
        {
            public int Id { get; set; }
            public string Title { get; set; }
        }
        public IllustSeries Series { get; set; }

        public class MetaSingle
        {
            public Uri OriginalImageUrl { get; set; }
        }
        public MetaSingle MetaSinglePage { get; set; }

        public class Meta
        {
            public SizedImageUrls ImageUrls { get; set; }
        }
        public Meta MetaPage { get; set; }
        public int TotalView { get; set; }
        public int TotalBookmarks { get; set; }
        public bool IsBookmarked { get; set; }
        public bool Visible { get; set; }
        public bool IsMuted { get; set; }
        public int TotalComments { get; set; }
    }

    public class SizedImageUrls
    {
        public Uri SquareMedium { get; set; }
        public Uri Medium { get; set; }
        public Uri Large { get; set; }
        public Uri Original { get; set; }
    }

    public class IllustTag
    {
        public string Name { get; set; }
        public string TranslatedName { get; set; }
    }
}
