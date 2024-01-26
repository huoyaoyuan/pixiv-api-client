using System;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class IllustPage
    {
        private readonly PixivClient _client;
        private readonly Uri _original, _medium, _squareMedium, _large;

        internal IllustPage(Illust illust, int index, PixivClient client, OriginalImageUrls urls)
        {
            Illust = illust;
            Index = index;
            _client = client;

            _original = urls.Original;
            _medium = urls.Medium;
            _squareMedium = urls.SquareMedium;
            _large = urls.Large;
        }

        internal IllustPage(Illust illust, int index, PixivClient client, PreviewImageUrls urls, Uri original)
        {
            Illust = illust;
            Index = index;
            _client = client;

            _original = original;
            _medium = urls.Medium;
            _squareMedium = urls.SquareMedium;
            _large = urls.Large;
        }

        public Illust Illust { get; }
        public int Index { get; }

        public ImageInfo Original => new(_original, _client);
        public ImageInfo Medium => new(_medium, _client);
        public ImageInfo SquareMedium => new(_squareMedium, _client);
        public ImageInfo Large => new(_large, _client);

        public ImageInfo AtSize(IllustSize size)
            => size switch
            {
                IllustSize.Original => Original,
                IllustSize.Medium => Medium,
                IllustSize.SquareMedium => SquareMedium,
                IllustSize.Large => Large,
                _ => throw new ArgumentException("Unknown enum value.", nameof(size))
            };
    }
}
