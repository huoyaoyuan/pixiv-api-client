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
    /// <summary>
    /// A state-less type to provide direct API call to Pixiv.
    /// </summary>
    /// <remarks>
    /// This type is state-less. Every call must be performed with access token.
    /// </remarks>
    public sealed class PixivApiClient : IDisposable
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new UnderscoreCaseNamingPolicy()
        };

        #region Constructors
        public PixivApiClient(HttpMessageHandler handler) => _httpClient = new HttpClient(handler);

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

        public async Task<(DateTimeOffset authTime, AuthResponse authResponse)> AuthAsync(string username, string password)
        {
            DateTimeOffset authTime = DateTimeOffset.Now;
            string time = authTime.ToString("yyyy-MM-ddTHH:mm:ssK", null);

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
                    sb.Append(b.ToString("x2", null));
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

            return (authTime, await AuthAsync(request).ConfigureAwait(false));
        }

        public async Task<(DateTimeOffset authTime, AuthResponse authResponse)> AuthAsync(string refreshToken)
        {
            DateTimeOffset authTime = DateTimeOffset.Now;

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

            return (authTime, await AuthAsync(request).ConfigureAwait(false));
        }

        private async Task<AuthResponse> AuthAsync(HttpRequestMessage request)
        {
            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                string original = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var error = JsonSerializer.Deserialize<PixivAuthErrorMessage>(original, s_serializerOptions);
                throw new PixivAuthException(original, error, error?.Errors?.System?.Message ?? original);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<AuthResponse>(s_serializerOptions).ConfigureAwait(false);

            return json ?? throw new InvalidOperationException("The api responses a null object.");
        }

        public ValueTask<(DateTimeOffset authTime, AuthResponse authResponse)> RefreshIfRequiredAsync(DateTimeOffset authTime, AuthResponse authResponse, int epsilonSeconds = 60)
        {
            if ((DateTimeOffset.UtcNow - authTime).TotalSeconds < authResponse.ExpiresIn - epsilonSeconds)
                return new((authTime, authResponse));

            return new(AuthAsync(authResponse.RefreshToken));
        }

        public Task<T> InvokeApiAsync<T>(
            string url,
            HttpMethod method,
            string? authToken = null,
            IEnumerable<KeyValuePair<string, string>>? additionalHeaders = null,
            HttpContent? body = null)
            => InvokeApiAsync<T>(
                new Uri(url),
                method,
                authToken,
                additionalHeaders,
                body);

        public async Task<T> InvokeApiAsync<T>(
            Uri url,
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

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                string original = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var error = JsonSerializer.Deserialize<PixivApiErrorMessage>(original, s_serializerOptions);
                throw new PixivApiException(original, error, error?.Error?.Message ?? original);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<T>(s_serializerOptions).ConfigureAwait(false);

            return json ?? throw new InvalidOperationException("The api responses a null object.");
        }

        public Task<UserDetail> GetUserDetailAsync(
            int userId,
            string? authToken = null)
        {
            return InvokeApiAsync<UserDetail>(
                $"https://app-api.pixiv.net/v1/user/detail?user_id={userId}",
                HttpMethod.Get,
                authToken: authToken);
        }

        public Task<UserIllusts> GetUserIllustsAsync(
            int userId,
            string illustType = "illust",
            int offset = 0,
            string? authToken = null)
        {
            return InvokeApiAsync<UserIllusts>(
                $"https://app-api.pixiv.net/v1/user/illusts?user_id={userId}"
                + $"&type={HttpUtility.UrlEncode(illustType)}&offset={offset}",
                HttpMethod.Get,
                authToken: authToken);
        }

        public Task<UserIllusts> GetUserBookmarkIllustsAsync(
            int userId,
            string restrict = "public",
            int? maxBookmarkId = null,
            string? tag = null,
            string? authToken = null)
        {
            string url = $"https://app-api.pixiv.net/v1/user/bookmarks/illust?user_id={userId}&restrict={HttpUtility.UrlEncode(restrict)}";
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
            IEnumerable<int>? seedIllustIds = null,
            string? authToken = null)
        {
            string url = $"https://app-api.pixiv.net/v2/illust/related?illust_id={illustId}";
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
                + $"&offset={offset}"
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

        public Task<UserIllusts> GetIllustRankingAsync(
            IllustRankingMode mode = IllustRankingMode.Day,
            DateTime? date = null,
            int offset = 0,
            string? authToken = null)
        {
            string url = $"https://app-api.pixiv.net/v1/illust/ranking?mode={mode.ToQueryString()}"
                + $"&offset={offset}";
            if (date is DateTime d)
                url += $"&date={d:yyyy-MM-dd}";

            return InvokeApiAsync<UserIllusts>(
                url,
                HttpMethod.Get,
                authToken);
        }

        public Task<TrendingTagsIllust> GetTrendingTagsIllustAsync(
            string? authToken = null)
        {
            return InvokeApiAsync<TrendingTagsIllust>(
                $"https://app-api.pixiv.net/v1/trending-tags/illust",
                HttpMethod.Get,
                authToken);
        }

        public Task<SearchIllustResult> SearchIllustsAsync(
            string word,
            IllustSearchTarget searchTarget = IllustSearchTarget.ExactTag,
            IllustSortMode sort = IllustSortMode.Latest,
            int? maxBookmarkCount = null,
            int? minBookmarkCount = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int offset = 0,
            string? authToken = null)
        {
            string url = $"https://app-api.pixiv.net/v1/search/illust?word={HttpUtility.UrlEncode(word)}&search_target={searchTarget.ToQueryString()}"
                + $"&sort={sort.ToQueryString()}&offset={offset}";
            if (maxBookmarkCount is int max)
                url += $"&bookmark_num_max={max}";
            if (minBookmarkCount is int min)
                url += $"&bookmark_num_min={min}";
            if (startDate is not null)
                url += $"&start_date={startDate:yyyy-MM-dd}";
            if (endDate is not null)
                url += $"&end_date={endDate:yyyy-MM-dd}";

            return InvokeApiAsync<SearchIllustResult>(
                url,
                HttpMethod.Get,
                authToken);
        }

        public Task AddIllustBookmarkAsync(
            int illustId,
            string restrict = "public",
            IEnumerable<string>? tags = null,
            string? authToken = null)
        {
            var data = new Dictionary<string, string>
            {
                ["illust_id"] = illustId.ToString(NumberFormatInfo.InvariantInfo),
                ["restrict"] = restrict
            };
            if (tags != null)
#if NETCOREAPP
                data.Add("tags", string.Join(' ', tags));
#else
                data.Add("tags", string.Join(" ", tags));
#endif

            return InvokeApiAsync<object>(
                "https://app-api.pixiv.net/v2/illust/bookmark/add",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(data!));
        }

        public Task DeleteIllustBookmarkAsync(
            int illustId,
            string? authToken = null)
        {
            return InvokeApiAsync<object>(
                "https://app-api.pixiv.net/v1/illust/bookmark/delete",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string?, string?>("illust_id", illustId.ToString(NumberFormatInfo.InvariantInfo))
                }));
        }

        public Task<UserBookmarkTags> GetUserBookmarkTagsIllustAsync(
            string restrict = "public",
            int offset = 0,
            string? authToken = null)
        {
            return InvokeApiAsync<UserBookmarkTags>(
                $"https://app-api.pixiv.net/v1/user/bookmark-tags/illust?restrict={HttpUtility.UrlEncode(restrict)}&offset={offset}",
                HttpMethod.Get,
                authToken);
        }

        public Task<UserFollowList> GetUserFollowingsAsync(
            int userId,
            string restrict = "public",
            int offset = 0,
            string? authToken = null)
        {
            return InvokeApiAsync<UserFollowList>(
                $"https://app-api.pixiv.net/v1/user/following?user_id={userId}&restrict={HttpUtility.UrlEncode(restrict)}&offset={offset}",
                HttpMethod.Get,
                authToken);
        }

        public Task<UserFollowList> GetUserFollowersAsync(
            int userId,
            string restrict = "public",
            int offset = 0,
            string? authToken = null)
        {
            return InvokeApiAsync<UserFollowList>(
                $"https://app-api.pixiv.net/v1/user/follower?user_id={userId}&restrict={HttpUtility.UrlEncode(restrict)}&offset={offset}",
                HttpMethod.Get,
                authToken);
        }

        public Task AddUserFollowAsync(
            int userId,
            string restrict = "public",
            string? authToken = null)
        {
            return InvokeApiAsync<object>(
                "https://app-api.pixiv.net/v1/user/follow/add",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(NumberFormatInfo.InvariantInfo),
                    ["restrict"] = restrict
                }!));
        }

        public Task DeleteUserFollowAsync(
            int userId,
            string restrict = "public",
            string? authToken = null)
        {
            return InvokeApiAsync<object>(
                "https://app-api.pixiv.net/v1/user/follow/delete",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(NumberFormatInfo.InvariantInfo),
                    ["restrict"] = restrict
                }!));
        }

        public Task<UserFollowList> GetMyPixivUsersAsync(
            int userId,
            int offset = 0,
            string? authToken = null)
        {
            return InvokeApiAsync<UserFollowList>(
                $"https://app-api.pixiv.net/v1/user/mypixiv?user_id={userId}&offset={offset}",
                HttpMethod.Get,
                authToken);
        }

        // api v2 doesn't return the same structure with v1

        //public Task<UserFollowList> GetBlockedUsersAsync(
        //    int userId,
        //    string filter = "for_ios",
        //    int offset = 0,
        //    string? authToken = null)
        //{
        //    return InvokeApiAsync<UserFollowList>(
        //        $"https://app-api.pixiv.net/v2/user/list?user_id={userId}&filter={HttpUtility.UrlEncode(filter)}&offset={offset}",
        //        HttpMethod.Get,
        //        authToken);
        //}

        public Task<MotionPicMetadata> GetMotionPicMetadataAsync(
            int illustId,
            string? authToken = null)
        {
            return InvokeApiAsync<MotionPicMetadata>(
                $"https://app-api.pixiv.net/v1/ugoira/metadata?illust_id={illustId}",
                HttpMethod.Get,
                authToken);
        }
    }
}
