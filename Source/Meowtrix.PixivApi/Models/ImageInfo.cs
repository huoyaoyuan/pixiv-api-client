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
        private readonly PixivClient _client;

        public ImageInfo(Uri uri, PixivClient client)
        {
            Uri = uri;
            _client = client;
        }

        public Task<HttpResponseMessage> RequestAsync(CancellationToken cancellation = default)
            => _client.Api.GetImageAsync(Uri, cancellation);

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
