﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IdentityModel.Client;
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
    public sealed class PixivApiClient : HttpClient
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
            }
        };

        #region Constructors
        public PixivApiClient(HttpMessageHandler handler)
            : base(handler)
        {
            BaseAddress = s_baseUri;
#if NET5_0_OR_GREATER
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
#endif
        }

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
            : base(new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = useProxy
            })
        {
            BaseAddress = s_baseUri;
#if NET5_0_OR_GREATER
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
#endif
        }
        #endregion

        private const string BaseUrl = "https://app-api.pixiv.net/";
        private static readonly Uri s_baseUri = new(BaseUrl);
        private const string ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
        private const string ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
        private const string UserAgent = "PixivAndroidApp/5.0.166 (Android 12.0)";
        private const string HashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";
        private const string AuthUrl = "https://oauth.secure.pixiv.net/auth/token";


        private static string MD5Hash(string input)
        {
#if NET5_0_OR_GREATER
            byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
#else
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
#endif

#if NETCOREAPP
            return Convert.ToHexString(bytes).ToLowerInvariant();
#else
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2", null));
            return sb.ToString();
#endif
        }


        [Obsolete("Authentication with username and password has been abandoned by Pixiv.")]
        public async Task<(DateTimeOffset authTime, AuthResponse authResponse)> AuthAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            DateTimeOffset authTime = DateTimeOffset.Now;
            string time = authTime.ToString("yyyy-MM-ddTHH:mm:ssK", null);

            static string MD5Hash(string input)
            {
#if NET5_0_OR_GREATER
                byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
#else
                using var md5 = MD5.Create();
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
#endif

#if NETCOREAPP
                return Convert.ToHexString(bytes).ToLowerInvariant();
#else
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2", null));
                return sb.ToString();
#endif
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl)
            {
                Content = new FormUrlEncodedContent(
                [
                    new ("get_secure_url", "1"),
                    new ("client_id", ClientId),
                    new ("client_secret", ClientSecret),
                    new ("grant_type", "password"),
                    new ("username", username),
                    new ("password", password),
                ]),
                Headers =
                {
                    { "User-Agent", UserAgent },
                    { "X-Client-Time", time },
                    { "X-Client-Hash", MD5Hash(time + HashSecret) }
                },
#if NET5_0_OR_GREATER
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
#endif
            };

            return (authTime, await AuthAsync(request, cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Generate authentication request for OAuth PKCE. See remarks for usage.
        /// </summary>
        /// <returns>
        /// Code verify and login url.
        /// </returns>
        /// <remarks>
        /// To perform login, call this method first. Invoke the LoginUrl in some browser view.
        /// Then listen for jump request to pixiv://....?code=.... , extract the code query,
        /// pass the code with CodeVerify into <see cref="CompleteAuthAsync"/>.
        /// The code is very short-live and should be passed immediately.
        /// </remarks>
#pragma warning disable CA1822
        public (string CodeVerify, string LoginUrl) BeginAuth()
#pragma warning restore CA1822
        {
#if NETCOREAPP
            Span<byte> bytes = stackalloc byte[36];
            RandomNumberGenerator.Fill(bytes);
#else
            byte[] bytes = new byte[36];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetNonZeroBytes(bytes);
#endif

            string codeVerifyString = Convert.ToBase64String(bytes);

#if NETCOREAPP
            Span<byte> codeVerify = stackalloc byte[48];
            Encoding.UTF8.GetBytes(codeVerifyString, codeVerify);
#else
            byte[] codeVerify = Encoding.UTF8.GetBytes(codeVerifyString);
#endif

#if NET5_0_OR_GREATER
            Span<byte> sha = stackalloc byte[32];
            SHA256.HashData(codeVerify, sha);
#elif NETCOREAPP3_1
            Span<byte> sha = stackalloc byte[32];
            using (var sha256 = SHA256.Create())
                sha256.TryComputeHash(codeVerify, sha, out _);
#else
            byte[] sha;
            using (var sha256 = SHA256.Create())
                sha = sha256.ComputeHash(codeVerify);
#endif
            string urlSafeCodeChallenge = Convert.ToBase64String(sha)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            string loginUrl = $"https://app-api.pixiv.net/web/v1/login?code_challenge={urlSafeCodeChallenge}&code_challenge_method=S256&client=pixiv-android";

            return (codeVerifyString, loginUrl);
        }

        /// <summary>
        /// Complete OAuth PKCE authentication. Used together with <see cref="BeginAuth"/>.
        /// </summary>
        /// <param name="code">Auth code invoked from login url.</param>
        /// <param name="codeVerify">Code verify returned from <see cref="BeginAuth"/>.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Authentication response.</returns>
        public async Task<(DateTimeOffset authTime, AuthResponse authResponse)> CompleteAuthAsync(string code, string codeVerify, CancellationToken cancellationToken = default)
        {
            DateTimeOffset authTime = DateTimeOffset.Now;

            using var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl)
            {
                Content = new FormUrlEncodedContent(
                [
                    new("code", code),
                    new("redirect_uri", "https://app-api.pixiv.net/web/v1/users/auth/pixiv/callback"),
                    new("grant_type", "authorization_code"),
                    new("include_policy", "true"),
                    new("client_id", ClientId),
                    new("client_secret", ClientSecret),
                    new("code_verifier", codeVerify)
                ]),
                Headers =
                {
                    { "User-Agent", UserAgent }
                },
#if NET5_0_OR_GREATER
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
#endif
            };

            return (authTime, await AuthAsync(request, cancellationToken).ConfigureAwait(false));
        }

        public async Task<(DateTimeOffset authTime, AuthResponse authResponse)> AuthAsync(
            string refreshToken,
            CancellationToken cancellation = default)
        {
            DateTimeOffset authTime = DateTimeOffset.Now;
            string requestTime = authTime.ToString(@"yyyy-MM-dd\THH\:mm\:ssK");

            using var request = new RefreshTokenRequest
            {
                Address = AuthUrl,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                RefreshToken = refreshToken,
                Headers =
                {
                    { "User-Agent", UserAgent },
                    { "X-Client-Time", requestTime },
                    { "X-Client-Hash", MD5Hash(requestTime + HashSecret) },
                }
            };

            var response = await this.RequestRefreshTokenAsync(request, cancellation).ConfigureAwait(false);

            return (authTime, new AuthResponse(response.AccessToken!, response.ExpiresIn, response.TokenType!,
                response.Scope!, response.RefreshToken!, response.Json!.Value.GetProperty("user").Deserialize<AuthUser>(s_serializerOptions)!));
        }

        private async Task<AuthResponse> AuthAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);

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
            string? authToken,
            string url,
            HttpMethod method,
            IEnumerable<KeyValuePair<string, string>>? additionalHeaders = null,
            HttpContent? body = null,
            CancellationToken cancellation = default)
            => InvokeApiAsync<T>(
                authToken,
                new Uri(url, UriKind.RelativeOrAbsolute),
                method,
                additionalHeaders,
                body,
                cancellation);

        public async Task<T> InvokeApiAsync<T>(
            string? authToken,
            Uri url,
            HttpMethod method,
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
                },
#if NET5_0_OR_GREATER
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
#endif
            };

            if (authToken is not null)
                request.Headers.Add("Authorization", $"Bearer {authToken}");
            if (additionalHeaders is not null)
                foreach (var header in additionalHeaders)
                    request.Headers.Add(header.Key, header.Value);

            using var response = await SendAsync(request, cancellation).ConfigureAwait(false);

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
            string? authToken,
            int userId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserDetail>(
                authToken: authToken,
                url: $"/v1/user/detail?user_id={userId}",
                method: HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<IllustDetailResponse> GetIllustDetailAsync(
            string? authToken,
            int illustId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<IllustDetailResponse>(
                authToken: authToken,
                url: $"/v1/illust/detail?illust_id={illustId}",
                method: HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetUserIllustsAsync(
            string? authToken,
            int userId,
            UserIllustType? illustType = null,
            int offset = 0,
            CancellationToken cancellation = default)
        {
            string url = $"/v1/user/illusts?user_id={userId}&offset={offset}";
            if (illustType is UserIllustType type)
                url += $"&type={type.ToQueryString()}";
            return InvokeApiAsync<UserIllusts>(
                authToken: authToken,
                url: url,
                method: HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetUserBookmarkIllustsAsync(
            string? authToken,
            int userId,
            Visibility restrict = Visibility.Public,
            int? maxBookmarkId = null,
            string? tag = null,
            CancellationToken cancellation = default)
        {
            string url = $"/v1/user/bookmarks/illust?user_id={userId}&restrict={restrict.ToQueryString()}";
            if (maxBookmarkId != null)
                url += $"&max_bookmark_id={maxBookmarkId}";
            if (!string.IsNullOrWhiteSpace(tag))
                url += $"&tag={HttpUtility.UrlEncode(tag.Trim())}";
            return InvokeApiAsync<UserIllusts>(
                authToken,
                url,
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetIllustFollowAsync(
            string? authToken,
            Visibility restrict = Visibility.Public,
            int offset = 0,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserIllusts>(
                authToken,
                $"/v2/illust/follow?restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<IllustComments> GetIllustCommentsAsync(
            string? authToken,
            int illustId,
            int offset = 0,
            bool includeTotalComments = false,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<IllustComments>(
                authToken,
                $"/v1/illust/comments?illust_id={illustId}&offset={offset}&include_total_comments={(includeTotalComments ? "true" : "false")}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<PostIllustCommentResult> PostIllustCommentAsync(
            string? authToken,
            int illustId,
            string comment,
            int? parentCommentId = null,
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
                authToken,
                "/v1/illust/comment/add",
                HttpMethod.Post,
                body: new FormUrlEncodedContent(data),
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetIllustRelatedAsync(
            string? authToken,
            int illustId,
            IEnumerable<int>? seedIllustIds = null,
            CancellationToken cancellation = default)
        {
            string url = $"/v2/illust/related?illust_id={illustId}";
            if (seedIllustIds != null)
                foreach (int seed in seedIllustIds)
                    url += $"&seed_illust_ids[]={seed}";

            return InvokeApiAsync<UserIllusts>(
                authToken,
                url,
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<RecommendedIllusts> GetRecommendedIllustsAsync(
            string? authToken,
            UserIllustType contentType = UserIllustType.Illustrations,
            bool includeRankingLabel = true,
            int? maxBookmarkIdForRecommended = null,
            int? minBookmarkIdForRecentIllust = null,
            int offset = 0,
            bool includeRankingIllusts = false,
            IEnumerable<int>? bookmarkIllustIds = null,
            bool includePrivacyPolicy = false,
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
                authToken,
                url,
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UserIllusts> GetIllustRankingAsync(
            string? authToken,
            IllustRankingMode mode = IllustRankingMode.Day,
            DateOnly? date = null,
            int offset = 0,
            CancellationToken cancellation = default)
        {
            string url = $"/v1/illust/ranking?mode={mode.ToQueryString()}"
                + $"&offset={offset}";
            if (date is not null)
                url += $"&date={date:yyyy-MM-dd}";

            return InvokeApiAsync<UserIllusts>(
                authToken,
                url,
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<TrendingTagsIllust> GetTrendingTagsIllustAsync(
            string? authToken,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<TrendingTagsIllust>(
                authToken,
                $"/v1/trending-tags/illust",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<SearchIllustResult> SearchIllustsAsync(
            string? authToken,
            string word,
            IllustSearchTarget searchTarget = IllustSearchTarget.ExactTag,
            IllustSortMode sort = IllustSortMode.Latest,
            int? maxBookmarkCount = null,
            int? minBookmarkCount = null,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            int offset = 0,
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
                authToken,
                url,
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UsersList> SearchUsersAsync(
            string? authToken,
            string word,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                authToken,
                $"/v1/search/user?word={word}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task AddIllustBookmarkAsync(
            string? authToken,
            int illustId,
            Visibility restrict = Visibility.Public,
            IEnumerable<string>? tags = null,
            CancellationToken cancellation = default)
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
                authToken,
                "/v2/illust/bookmark/add",
                HttpMethod.Post,
                body: new FormUrlEncodedContent(data),
                cancellation: cancellation);
        }

        public Task DeleteIllustBookmarkAsync(
            string? authToken,
            int illustId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<object>(
                authToken,
                "/v1/illust/bookmark/delete",
                HttpMethod.Post,
                body: new FormUrlEncodedContent(
                [
                    new("illust_id", illustId.ToString(NumberFormatInfo.InvariantInfo))
                ]),
                cancellation: cancellation);
        }

        public Task<UserBookmarkTags> GetUserBookmarkTagsIllustAsync(
            string? authToken,
            Visibility restrict = Visibility.Public,
            int offset = 0,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserBookmarkTags>(
                authToken,
                $"/v1/user/bookmark-tags/illust?restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UsersList> GetUserFollowingsAsync(
            string? authToken,
            int userId,
            Visibility restrict = Visibility.Public,
            int offset = 0,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                authToken,
                $"/v1/user/following?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UsersList> GetUserFollowersAsync(
            string? authToken,
            int userId,
            Visibility restrict = Visibility.Public,
            int offset = 0,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                authToken,
                $"/v1/user/follower?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task AddUserFollowAsync(
            string? authToken,
            int userId,
            Visibility restrict = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<object>(
                authToken,
                "/v1/user/follow/add",
                HttpMethod.Post,
                body: new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(NumberFormatInfo.InvariantInfo),
                    ["restrict"] = restrict.ToQueryString()
                }),
                cancellation: cancellation);
        }

        public Task DeleteUserFollowAsync(
            string? authToken,
            int userId,
            Visibility restrict = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<object>(
                authToken,
                "/v1/user/follow/delete",
                HttpMethod.Post,
                body: new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(NumberFormatInfo.InvariantInfo),
                    ["restrict"] = restrict.ToQueryString()
                }),
                cancellation: cancellation);
        }

        public Task<UsersList> GetMyPixivUsersAsync(
            string? authToken,
            int userId,
            int offset = 0,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UsersList>(
                authToken,
                $"/v1/user/mypixiv?user_id={userId}&offset={offset}",
                HttpMethod.Get,
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
            string? authToken,
            int illustId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<AnimatedPictureMetadata>(
                authToken,
                $"/v1/ugoira/metadata?illust_id={illustId}",
                HttpMethod.Get,
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
                },
#if NET5_0_OR_GREATER
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
#endif
            };

            return (await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
                .EnsureSuccessStatusCode();
        }

        public ValueTask<T?> GetNextPageAsync<T>(string? authToken, T previous,
            CancellationToken cancellation = default)
            where T : class, IHasNextPage
        {
            if (previous.NextUrl is null)
                return default;

            // TODO: nullable convariance of task
            return new(InvokeApiAsync<T>(authToken, previous.NextUrl, HttpMethod.Get, cancellation: cancellation)!);
        }

        public Task<IllustSeriesInfo> GetIllustSeriesAsync(
            string? authToken,
            int illustSeriesId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<IllustSeriesInfo>(
                authToken,
                $"/v1/illust/series?illust_series_id={illustSeriesId}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UserIllustSeries> GetUserIllustSeriesAsync(
            string? authToken,
            int userId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserIllustSeries>(
                authToken,
                $"/v1/user/illust-series?user_id={userId}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<UserNovels> GetUserNovelsAsync(
            string? authToken,
            int userId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<UserNovels>(
                authToken,
                $"/v1/user/novels?user_id={userId}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<NovelDetailResponse> GetNovelDetailAsync(
            string? authToken,
            int novelId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<NovelDetailResponse>(
                authToken,
                $"/v2/novel/detail?novel_id={novelId}",
                HttpMethod.Get,
                cancellation: cancellation);
        }

        public Task<NovelTextResponse> GetNovelTextAsync(
            string? authToken,
            int novelId,
            CancellationToken cancellation = default)
        {
            return InvokeApiAsync<NovelTextResponse>(
                authToken,
                $"/v1/novel/text?novel_id={novelId}",
                HttpMethod.Get,
                cancellation: cancellation);
        }
    }
}
