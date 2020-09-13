using System;
using System.IO;
using System.Net.Http;
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

        public Task<HttpResponseMessage> RequestAsync() => _api.GetImageAsync(Uri);

        public async Task<Stream> RequestStreamAsync()
        {
            var response = await RequestAsync().ConfigureAwait(false);
            return await response
                .EnsureSuccessStatusCode()
                .Content.ReadAsStreamAsync()
                .ConfigureAwait(false);
        }
    }
}
