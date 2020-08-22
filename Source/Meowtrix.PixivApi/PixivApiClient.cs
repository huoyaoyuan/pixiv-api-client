using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        private const string ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
        private const string ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
        private const string UserAgent = "PixivAndroidApp/5.0.64 (Android 6.0)";
        private const string HashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";

        public async Task AuthAsync(string username, string password)
        {
            const string Url = "https://oauth.secure.pixiv.net/auth/token";
#pragma warning disable CA1305 // 指定 IFormatProvider
            string time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00");
#pragma warning restore CA1305 // 指定 IFormatProvider

            static string MD5Hash(string input)
            {
#pragma warning disable CA5351 // 不要使用损坏的加密算法
                using var md5 = MD5.Create();
#pragma warning restore CA5351 // 不要使用损坏的加密算法
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

#if NETCOREAPP
                return string.Create(bytes.Length * 2, bytes, (span, b) =>
                {
                    for (int i = 0; i < b.Length; i++)
                        _ = b[i].TryFormat(span[(i * 2)..], out _, "x2");
                });
#else
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
#pragma warning disable CA1305 // 指定 IFormatProvider
                    sb.Append(b.ToString("x2"));
#pragma warning restore CA1305 // 指定 IFormatProvider
                return sb.ToString();
#endif
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = new FormUrlEncodedContent(new KeyValuePair<string?, string?>[]
                {
                    new ("get_secure_url", "1"),
                    new ("client_id", ClientId),
                    new ("client_secret", ClientSecret),
                    new ("grant_type", "password"),
                    new ("username", username),
                    new ("password", password),
                }),
                Headers =
                {
                    { "User-Agent", UserAgent },
                    { "X-Client-Time", time },
                    { "X-Client-Hash", MD5Hash(time + HashSecret) }
                }
            };

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var json = response.Content.ReadFromJsonAsync<object>(s_serializerOptions);
        }
    }
}
