using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi
{
    /// <summary>
    /// A stateless type to provide direct API call to Pixiv.
    /// </summary>
    /// <remarks>
    /// This type is stateless. Every call must be performed with access token.
    /// For stateful usage, please use <see cref="PixivClient"/> instead.
    /// </remarks>
    public sealed class PixivApiClient : IDisposable
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new UnderscoreCaseNamingPolicy(),
            Converters =
            {
                new JsonStringEnumConverter(new UnderscoreCaseNamingPolicy())
            }
        };

        #region Constructors
        public PixivApiClient(HttpMessageHandler handler)
            => _httpClient = new HttpClient(handler)
            {
                BaseAddress = s_baseUri
            };

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
#pragma warning disable CA5399 // false positive on net461
            _httpClient = new HttpClient(new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = useProxy
            })
            {
                BaseAddress = s_baseUri
            };
        }
#pragma warning restore CA5399
        #endregion

        private readonly HttpClient _httpClient;
        public void Dispose() => _httpClient.Dispose();

        private const string BaseUrl = "https://app-api.pixiv.net/";
        private static readonly Uri s_baseUri = new Uri(BaseUrl);
        private const string ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
        private const string ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
        private const string UserAgent = "PixivAndroidApp/5.0.64 (Android 6.0)";
        private const string HashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";
        private const string AuthUrl = "https://oauth.secure.pixiv.net/auth/token";

        public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

        public async Task<(DateTimeOffset authTime, AuthResponse authResponse)> AuthAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
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

            return (authTime, await AuthAsync(request, cancellationToken).ConfigureAwait(false));
        }

        public async Task<(DateTimeOffset authTime, AuthResponse authResponse)> AuthAsync(
            string refreshToken,
            CancellationToken cancellation = default)
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

            return (authTime, await AuthAsync(request, cancellation).ConfigureAwait(false));
        }

        private async Task<AuthResponse> AuthAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                string original = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var error = JsonSerializer.Deserialize<PixivAuthErrorMessage>(original, s_serializerOptions);
                throw new PixivAuthException(original, error, error?.Errors?.System?.Message ?? original);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<AuthResponse>(s_serializerOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

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
            HttpContent? body = null,
            CancellationToken cancellation = default)
            => InvokeApiAsync<T>(
                new Uri(url, UriKind.RelativeOrAbsolute),
                method,
                authToken,
                additionalHeaders,
                body,
                cancellation);

        public async Task<T> InvokeApiAsync<T>(
            Uri url,
            HttpMethod method,
            string? authToken = null,
            IEnumerable<KeyValuePair<string, string>>? additionalHeaders = null,
            HttpContent? body = null,
            CancellationToken cancellation = default)
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

            using var response = await _httpClient.SendAsync(request, cancellation).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                string original = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
                var error = JsonSerializer.Deserialize<PixivApiErrorMessage>(original, s_serializerOptions);
                throw new PixivApiException(original, error, error?.Error?.Message ?? original);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<T>(s_serializerOptions, cancellationToken: cancellation).ConfigureAwait(false);

            return json ?? throw new InvalidOperationException("The api responses a null object.");
        }

        public Task<UserDetail> GetUserDetailAsync(
            int userId,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserDetail>(
                $"/v1/user/detail?user_id={userId}",
                HttpMethod.Get,
                authToken: authToken,
                cancellation: cancellation);
        }

        public Task<IllustDetailResponse> GetIllustDetailAsync(
            int illustId,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<IllustDetailResponse>(
                $"/v1/illust/detail?illust_id={illustId}",
                HttpMethod.Get,
                authToken: authToken,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetUserIllustsAsync(
            int userId,
            UserIllustType illustType = UserIllustType.Illustrations,
            int offset = 0,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserIllusts>(
                $"/v1/user/illusts?user_id={userId}"
                + $"&type={illustType.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                authToken: authToken,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetUserBookmarkIllustsAsync(
            int userId,
            Visibility restrict = Visibility.Public,
            int? maxBookmarkId = null,
            string? tag = null,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            string url = $"/v1/user/bookmarks/illust?user_id={userId}&restrict={restrict.ToQueryString()}";
            if (maxBookmarkId != null)
                url += $"&max_bookmark_id={maxBookmarkId}";
            if (!string.IsNullOrWhiteSpace(tag))
                url += $"&tag={HttpUtility.UrlEncode(tag.Trim())}";
            return InvokeApiAsync<UserIllusts>(
                url,
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetIllustFollowAsync(
            Visibility restrict = Visibility.Public,
            int offset = 0,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserIllusts>(
                $"/v2/illust/follow?restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task<IllustComments> GetIllustCommentsAsync(
            int illustId,
            int offset = 0,
            bool includeTotalComments = false,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<IllustComments>(
                $"/v1/illust/comments?illust_id={illustId}&offset={offset}&include_total_comments={(includeTotalComments ? "true" : "false")}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task<PostIllustCommentResult> PostIllustCommentAsync(
            int illustId,
            string comment,
            int? parentCommentId = null,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            var data = new Dictionary<string, string>
            {
                ["illust_id"] = illustId.ToString(NumberFormatInfo.InvariantInfo),
                ["comment"] = comment
            };

            if (parentCommentId is int p)
                data.Add("parent_comment_id", p.ToString(NumberFormatInfo.InvariantInfo));

            return InvokeApiAsync<PostIllustCommentResult>(
                "/v1/illust/comment/add",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(data!),
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetIllustRelatedAsync(
            int illustId,
            IEnumerable<int>? seedIllustIds = null,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            string url = $"/v2/illust/related?illust_id={illustId}";
            if (seedIllustIds != null)
                foreach (int seed in seedIllustIds)
                    url += $"&seed_illust_ids[]={seed}";

            return InvokeApiAsync<UserIllusts>(
                url,
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task<RecommendedIllusts> GetRecommendedIllustsAsync(
            UserIllustType contentType = UserIllustType.Illustrations,
            bool includeRankingLabel = true,
            int? maxBookmarkIdForRecommended = null,
            int? minBookmarkIdForRecentIllust = null,
            int offset = 0,
            bool includeRankingIllusts = false,
            IEnumerable<int>? bookmarkIllustIds = null,
            bool includePrivacyPolicy = false,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            string url = authToken is null
                ? "/v1/illust/recommended-nologin"
                : "/v1/illust/recommended";
            url += $"?content_type={contentType.ToQueryString()}"
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
                authToken,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetIllustRankingAsync(
            IllustRankingMode mode = IllustRankingMode.Day,
            DateTime? date = null,
            int offset = 0,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            string url = $"/v1/illust/ranking?mode={mode.ToQueryString()}"
                + $"&offset={offset}";
            if (date is DateTime d)
                url += $"&date={d:yyyy-MM-dd}";

            return InvokeApiAsync<UserIllusts>(
                url,
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task<TrendingTagsIllust> GetTrendingTagsIllustAsync(
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<TrendingTagsIllust>(
                $"/v1/trending-tags/illust",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
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
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            string url = $"/v1/search/illust?word={HttpUtility.UrlEncode(word)}&search_target={searchTarget.ToQueryString()}"
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
                authToken,
                cancellation: cancellation);
        }

        public Task<UsersList> SearchUsersAsync(
            string word,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                $"/v1/search/user?word={word}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task AddIllustBookmarkAsync(
            int illustId,
            Visibility restrict = Visibility.Public,
            IEnumerable<string>? tags = null,
            string? authToken = null)
        {
            var data = new Dictionary<string, string>
            {
                ["illust_id"] = illustId.ToString(NumberFormatInfo.InvariantInfo),
                ["restrict"] = restrict.ToQueryString()
            };
            if (tags != null)
#if NETCOREAPP
                data.Add("tags", string.Join(' ', tags));
#else
                data.Add("tags", string.Join(" ", tags));
#endif

            return InvokeApiAsync<object>(
                "/v2/illust/bookmark/add",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(data!));
        }

        public Task DeleteIllustBookmarkAsync(
            int illustId,
            string? authToken = null)
        {
            return InvokeApiAsync<object>(
                "/v1/illust/bookmark/delete",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string?, string?>("illust_id", illustId.ToString(NumberFormatInfo.InvariantInfo))
                }));
        }

        public Task<UserBookmarkTags> GetUserBookmarkTagsIllustAsync(
            Visibility restrict = Visibility.Public,
            int offset = 0,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserBookmarkTags>(
                $"/v1/user/bookmark-tags/illust?restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task<UsersList> GetUserFollowingsAsync(
            int userId,
            Visibility restrict = Visibility.Public,
            int offset = 0,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                $"/v1/user/following?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task<UsersList> GetUserFollowersAsync(
            int userId,
            Visibility restrict = Visibility.Public,
            int offset = 0,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                $"/v1/user/follower?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public Task AddUserFollowAsync(
            int userId,
            Visibility restrict = Visibility.Public,
            string? authToken = null)
        {
            return InvokeApiAsync<object>(
                "/v1/user/follow/add",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(NumberFormatInfo.InvariantInfo),
                    ["restrict"] = restrict.ToQueryString()
                }!));
        }

        public Task DeleteUserFollowAsync(
            int userId,
            Visibility restrict = Visibility.Public,
            string? authToken = null)
        {
            return InvokeApiAsync<object>(
                "/v1/user/follow/delete",
                HttpMethod.Post,
                authToken,
                body: new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(NumberFormatInfo.InvariantInfo),
                    ["restrict"] = restrict.ToQueryString()
                }!));
        }

        public Task<UsersList> GetMyPixivUsersAsync(
            int userId,
            int offset = 0,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                $"/v1/user/mypixiv?user_id={userId}&offset={offset}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        // api v2 doesn't return the same structure with v1

        //public Task<UserFollowList> GetBlockedUsersAsync(
        //    int userId,
        //    string filter = "for_ios",
        //    int offset = 0,
        //    string? authToken = null)
        //{
        //    return InvokeApiAsync<UserFollowList>(
        //        $"/v2/user/list?user_id={userId}&filter={HttpUtility.UrlEncode(filter)}&offset={offset}",
        //        HttpMethod.Get,
        //        authToken);
        //}

        public Task<AnimatedPictureMetadata> GetAnimatedPictureMetadataAsync(
            int illustId,
            string? authToken = null,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<AnimatedPictureMetadata>(
                $"/v1/ugoira/metadata?illust_id={illustId}",
                HttpMethod.Get,
                authToken,
                cancellation: cancellation);
        }

        public async Task<HttpResponseMessage> GetImageAsync(Uri imageUri,
            CancellationToken cancellation = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, imageUri)
            {
                Headers =
                {
                    Referrer = s_baseUri
                }
            };

            return (await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
                .EnsureSuccessStatusCode();
        }

        public ValueTask<T?> GetNextPageAsync<T>(T previous, string? authToken = null,
            CancellationToken cancellation = default)
            where T : class, IHasNextPage
        {
            if (previous.NextUrl is null)
                return default;

            // TODO: nullable convariance of task
            return new(InvokeApiAsync<T>(previous.NextUrl, HttpMethod.Get, authToken, cancellation: cancellation)!);
        }
    }
}
