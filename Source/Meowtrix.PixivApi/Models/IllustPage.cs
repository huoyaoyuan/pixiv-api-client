using System;
using static Meowtrix.PixivApi.Json.IllustDetail;

namespace Meowtrix.PixivApi.Models
{
    public class IllustPage
    {
        private readonly PixivClient _client;
        private readonly Uri _original, _medium, _squareMedium, _large;

        internal IllustPage(PixivClient client, MetaPageImageUrls urls)
        {
            _client = client;

            _original = urls.Original;
            _medium = urls.Medium;
            _squareMedium = urls.SquareMedium;
            _large = urls.Large;
        }

        internal IllustPage(PixivClient client, PreviewImageUrls urls, Uri original)
        {
            _client = client;

            _original = original;
            _medium = urls.Medium;
            _squareMedium = urls.SquareMedium;
            _large = urls.Large;
        }

        public ImageInfo Original => new ImageInfo(_original, _client.Api);
        public ImageInfo Medium => new ImageInfo(_medium, _client.Api);
        public ImageInfo SquareMedium => new ImageInfo(_squareMedium, _client.Api);
        public ImageInfo Large => new ImageInfo(_large, _client.Api);

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
