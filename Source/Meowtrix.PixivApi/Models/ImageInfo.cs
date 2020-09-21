using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Meowtrix.PixivApi.Models
{
    public readonly struct ImageInfo
    {
        public Uri Uri { get; }
        private readonly PixivApiClient _api;

        public ImageInfo(Uri uri, PixivApiClient api)
        {
            Uri = uri;
            _api = api;
        }

        public Task<HttpResponseMessage> RequestAsync(CancellationToken cancellation = default)
            => _api.GetImageAsync(Uri, cancellation);

        public async Task<Stream> RequestStreamAsync(CancellationToken cancellation = default)
        {
            var response = await RequestAsync(cancellation).ConfigureAwait(false);
            return await response
                .EnsureSuccessStatusCode()
                .Content.ReadAsStreamAsync(cancellation)
                .ConfigureAwait(false);
        }
    }
}
