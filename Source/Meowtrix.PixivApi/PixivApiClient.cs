using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Meowtrix.PixivApi
{
    public sealed class PixivApiClient : IDisposable
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new UnderscoreCaseNamingPolicy()
        };

        #region Constructors
        public PixivApiClient()
            : this(false, null)
        {
        }

        public PixivApiClient(bool useDefaultProxy)
            : this(useDefaultProxy, null)
        {
        }

        public PixivApiClient(IWebProxy proxy)
            : this(true, proxy)
        {
        }

        private PixivApiClient(bool useProxy, IWebProxy? proxy)
        {
            _httpClient = new HttpClient(new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = useProxy
            });
        }
        #endregion

        private readonly HttpClient _httpClient;
        public void Dispose() => _httpClient.Dispose();
    }
}
