using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Meowtrix.PixivApi.Json;

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
        private const string AuthUrl = "https://oauth.secure.pixiv.net/auth/token";

        public async Task<(DateTimeOffset authTime, AuthResult authResponse)> AuthAsync(string username, string password)
        {
            DateTimeOffset authTime = DateTimeOffset.UtcNow;
#pragma warning disable CA1305 // 指定 IFormatProvider
            string time = authTime.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss+00:00");
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

            using var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl)
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
            var json = await response.Content.ReadFromJsonAsync<AuthResult>(s_serializerOptions).ConfigureAwait(false);

            return (authTime, json ?? throw new InvalidOperationException("Bad authentication response."));
        }

        public async Task<(DateTimeOffset authTime, AuthResult authResponse)> AuthAsync(string refreshToken)
        {
            DateTimeOffset authTime = DateTimeOffset.UtcNow;

            using var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl)
            {
                Content = new FormUrlEncodedContent(new KeyValuePair<string?, string?>[]
                {
                    new ("get_secure_url", "1"),
                    new ("client_id", ClientId),
                    new ("client_secret", ClientSecret),
                    new ("grant_type", "refresh_token"),
                    new ("refresh_token", refreshToken),
                }),
                Headers =
                {
                    { "User-Agent", UserAgent }
                }
            };

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var json = await response.Content.ReadFromJsonAsync<AuthResult>(s_serializerOptions).ConfigureAwait(false);

            return (authTime, json ?? throw new InvalidOperationException("Bad authentication response."));
        }

        public ValueTask<(DateTimeOffset authTime, AuthResult authResponse)> RefreshIfRequiredAsync(DateTimeOffset authTime, AuthResult authResponse, int epsilonSeconds = 60)
        {
            if ((DateTimeOffset.UtcNow - authTime).TotalSeconds < authResponse.Response.ExpiresIn - epsilonSeconds)
                return new((authTime, authResponse));

            return new(AuthAsync(authResponse.Response.RefreshToken));
        }

        private async Task<T> InvokeApiAsync<T>(
            string url,
            HttpMethod method,
            string? authToken = null,
            IEnumerable<KeyValuePair<string, string>>? additionalHeaders = null,
            HttpContent? body = null)
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = body,
                Headers =
                {
                    { "App-OS", "ios" },
                    { "App-OS-Version", "10.3.1" },
                    { "App-Version", "6.7.1" },
                    { "User-Agent", "PixivIOSApp/6.7.1 (iOS 10.3.1; iPhone8,1)" }
                }
            };

            if (authToken is not null)
                request.Headers.Add("Authorization", $"Bearer {authToken}");
            if (additionalHeaders is not null)
                foreach (var header in additionalHeaders)
                    request.Headers.Add(header.Key, header.Value);

            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            var json = await response.Content.ReadFromJsonAsync<T>(s_serializerOptions).ConfigureAwait(false);

            return json ?? throw new InvalidOperationException("Bad api response.");
        }

        public Task<UserDetail> GetUserDetailAsync(string userId, string filter = "for_ios", string? authToken = null)
        {
            return InvokeApiAsync<UserDetail>(
                $"https://app-api.pixiv.net/v1/user/detail?user_id={HttpUtility.UrlEncode(userId)}&filter={HttpUtility.UrlEncode(filter)}",
                HttpMethod.Get,
                authToken: authToken);
        }

        public Task<UserIllusts> GetUserIllustsAsync(
            string userId,
            string illustType = "illust",
            string filter = "for_ios",
            int offset = 0,
            string? authToken = null)
        {
            return InvokeApiAsync<UserIllusts>(
                $"https://app-api.pixiv.net/v1/user/illusts?user_id={HttpUtility.UrlEncode(userId)}&filter={HttpUtility.UrlEncode(filter)}"
                + $"&type={HttpUtility.UrlEncode(illustType)}$offset={offset}",
                HttpMethod.Get,
                authToken: authToken);
        }

        public Task<UserIllusts> GetUserBookmarkIllustsAsync(
            string userId,
            string restrict = "public",
            string filter = "for_ios",
            int? maxBookmarkId = null,
            string? tag = null,
            string? authToken = null)
        {
            string url = $"https://app-api.pixiv.net/v1/user/bookmarks/illust?user_id={HttpUtility.UrlEncode(userId)}&restrict={HttpUtility.UrlEncode(restrict)}&filter={HttpUtility.UrlEncode(filter)}";
            if (maxBookmarkId != null)
                url += $"&max_bookmark_id={maxBookmarkId}";
            if (!string.IsNullOrWhiteSpace(tag))
                url += $"&tag={HttpUtility.UrlEncode(tag.Trim())}";
            return InvokeApiAsync<UserIllusts>(
                url,
                HttpMethod.Get,
                authToken);
        }

        public Task<UserIllusts> GetIllustFollowAsync(
            string restrict = "public",
            int offset = 0,
            string? authToken = null)
        {
            return InvokeApiAsync<UserIllusts>(
                $"https://app-api.pixiv.net/v2/illust/follow?restrict={HttpUtility.UrlEncode(restrict)}&offset={offset}",
                HttpMethod.Get,
                authToken);
        }

        public Task<IllustComments> GetIllustCommentsAsync(
            int illustId,
            int offset = 0,
            bool includeTotalComments = false,
            string? authToken = null)
        {
            return InvokeApiAsync<IllustComments>(
                $"https://app-api.pixiv.net/v1/illust/comments?illust_id={illustId}&offset={offset}&include_total_comments={(includeTotalComments ? "true" : "false")}",
                HttpMethod.Get,
                authToken);
        }

        public Task<PostIllustCommentResult> PostIllustCommentAsync(
            int illustId,
            string comment,
            int? parentCommentId = null,
            string? authToken = null)
        {
            var data = new Dictionary<string, string>
            {
                ["illust_id"] = illustId.ToString(NumberFormatInfo.InvariantInfo),
                ["comment"] = comment
            };

            if (parentCommentId is int p)
                data.Add("parent_comment_id", p.ToString(NumberFormatInfo.InvariantInfo));

            return InvokeApiAsync<PostIllustCommentResult>(
                "https://app-api.pixiv.net/v1/illust/comment/add",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(data!));
        }

        public Task<UserIllusts> GetIllustRelatedAsync(
            int illustId,
            string filter = "for_ios",
            IEnumerable<int>? seedIllustIds = null,
            string? authToken = null)
        {
            string url = $"https://app-api.pixiv.net/v2/illust/related?illust_id={illustId}&filter={filter}";
            if (seedIllustIds != null)
                foreach (int seed in seedIllustIds)
                    url += $"&seed_illust_ids[]={seed}";

            return InvokeApiAsync<UserIllusts>(
                url,
                HttpMethod.Get,
                authToken);
        }

        public Task<RecommendedIllusts> GetRecommendedIllustsAsync(
            string contentType = "illust",
            bool includeRankingLabel = true,
            string filter = "for_ios",
            int? maxBookmarkIdForRecommended = null,
            int? minBookmarkIdForRecentIllust = null,
            int offset = 0,
            bool includeRankingIllusts = false,
            IEnumerable<int>? bookmarkIllustIds = null,
            bool includePrivacyPolicy = false,
            string? authToken = null)
        {
            string url = authToken is null
                ? "https://app-api.pixiv.net/v1/illust/recommended-nologin"
                : "https://app-api.pixiv.net/v1/illust/recommended";
            url += $"?content_type={HttpUtility.UrlEncode(contentType)}"
                + $"&filter={HttpUtility.UrlEncode(filter)}&offset={offset}"
                + $"&include_ranking_label={(includeRankingLabel ? "true" : "false")}"
                + $"&include_ranking_illusts={(includeRankingIllusts ? "true" : "false")}"
                + $"&include_privacy_policy={(includePrivacyPolicy ? "true" : "false")}";
            if (maxBookmarkIdForRecommended is int rId)
                url += $"&max_bookmark_id_for_recommend={rId}";
            if (minBookmarkIdForRecentIllust is int iId)
                url += $"&min_bookmark_id_for_recent_illust={iId}";
            if (bookmarkIllustIds != null)
#if NETCOREAPP
                url += $"&bookmark_illust_ids={string.Join(',', bookmarkIllustIds)}";
#else
                url += $"&bookmark_illust_ids={string.Join(",", bookmarkIllustIds)}";
#endif

            return InvokeApiAsync<RecommendedIllusts>(
                url,
                HttpMethod.Get,
                authToken);
        }
    }
}
