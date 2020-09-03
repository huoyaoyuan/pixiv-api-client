using System;
using System.Net.Http;
using System.Threading.Tasks;
using static Meowtrix.PixivApi.Json.UserIllustPreview;

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

        public Task<HttpResponseMessage> OpenImageAsync(IllustSize size)
            => _client.Api.GetImageAsync(size switch
            {
                IllustSize.Original => _original,
                IllustSize.Medium => _medium,
                IllustSize.SquareMedium => _squareMedium,
                IllustSize.Large => _large,
                _ => throw new ArgumentException("Unknown enum value.", nameof(size))
            });
    }
}
