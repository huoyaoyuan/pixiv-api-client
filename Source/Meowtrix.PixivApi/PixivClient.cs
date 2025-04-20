using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Meowtrix.PixivApi.Authentication;
using Meowtrix.PixivApi.Json;
using Meowtrix.PixivApi.Models;

namespace Meowtrix.PixivApi
{
    /// <summary>
    /// A stateful class to provide modeled pixiv access.
    /// </summary>
    /// <remarks>
    /// This class is stateful. Login and logout will be preserved.
    /// For stateless usage, please use <see cref="PixivApiClient"/> instead.
    /// </remarks>
    public sealed class PixivClient : IDisposable
    {
        private readonly AccessTokenManager _tokenManager = new(null);
        private PixivApiClient _apiClient;

        internal PixivApiClient Api => _apiClient;

        #region Construction and disposal
        public PixivClient(bool useDefaultProxy = true)
            => _apiClient = new PixivApiClient(_tokenManager, new HttpClientHandler { UseProxy = useDefaultProxy });

        public PixivClient(IWebProxy? proxy)
            => _apiClient = new PixivApiClient(_tokenManager,
                proxy is null
                ? new HttpClientHandler { UseProxy = false }
                : new HttpClientHandler { Proxy = proxy });

        public PixivClient(HttpMessageHandler handler)
            => _apiClient = new PixivApiClient(_tokenManager, handler);

        public PixivClient(PixivApiClient lowLevelClient)
            => _apiClient = lowLevelClient;

        public void SetProxy(IWebProxy? proxy)
        {
            ChangeApiClient(new PixivApiClient(_tokenManager,
                proxy is null
                ? new HttpClientHandler { UseProxy = false }
                : new HttpClientHandler { Proxy = proxy }));
        }

        public void SetDefaultProxy()
            => ChangeApiClient(new PixivApiClient(_tokenManager, new HttpClientHandler { UseProxy = true }));

        public void SetHandler(HttpMessageHandler handler)
            => ChangeApiClient(new PixivApiClient(_tokenManager, handler));

        private void ChangeApiClient(PixivApiClient newApiClient)
        {
            SetRequestHeader(newApiClient.HttpClient, RequestLanguage);
            var old = Interlocked.Exchange(ref _apiClient, newApiClient);
            old.Dispose();
        }

        public void Dispose() => Api.Dispose();
        #endregion

        [MemberNotNullWhen(true, nameof(CurrentUser))]
        public bool IsLogin => _tokenManager.IsAuthenticated;

        /// <summary>
        /// Perform login with OAuth PKCE.
        /// </summary>
        /// <param name="requestFunc">Accesses the url parameter in browser,
        /// listens and returns for pixiv:// request.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The returned refresh token.</returns>
        public async Task<string> LoginAsync(Func<string, CancellationToken, Task<Uri>> requestFunc, CancellationToken cancellationToken = default)
        {
            var (codeVerify, loginUrl) = PixivAuthentication.PrepareWebLogin();
            var uri = await requestFunc(loginUrl, cancellationToken).ConfigureAwait(false);

            if (uri.Scheme != "pixiv")
                throw new InvalidOperationException("The returned uri isn't for pixiv authentication.");

            var query = HttpUtility.ParseQueryString(uri.Query);
            string code = query["code"] ?? throw new InvalidOperationException("The login request doesn't contain code.");

            var authResult = await PixivAuthentication.CompleteWebLoginAsync(
                new HttpMessageInvoker(Api.InnerHandler, false),
                code, codeVerify,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            _tokenManager.Authenticate(authResult);
            CurrentUser = new(this, authResult.UserInfo);
            return authResult.RefreshToken;
        }

        /// <summary>
        /// Perform login with refresh token.
        /// </summary>
        /// <param name="refreshToken">The stored refresh token.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The returned refresh token.</returns>
        public async Task<string> LoginAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var authResult = await PixivAuthentication.AuthWithRefreshTokenAsync(
                new HttpMessageInvoker(Api.InnerHandler, false),
                refreshToken,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            _tokenManager.Authenticate(authResult);
            CurrentUser = new(this, authResult.UserInfo);
            return authResult.RefreshToken;
        }

        public LoginUser? CurrentUser { get; private set; }

        [MemberNotNull(nameof(CurrentUser))]
        public int CurrentUserId => CurrentUser?.Id ?? throw new InvalidOperationException("No user login.");

        private CultureInfo? _requestLanguage;
        public CultureInfo? RequestLanguage
        {
            get => _requestLanguage;
            set
            {
                if (_requestLanguage != value)
                {
                    _requestLanguage = value;
                    SetRequestHeader(Api.HttpClient, value);
                }
            }
        }

        private static void SetRequestHeader(HttpClient apiClient, CultureInfo? cultureInfo)
        {
            apiClient.DefaultRequestHeaders.AcceptLanguage.Clear();
            if (!string.IsNullOrEmpty(cultureInfo?.Name))
                apiClient.DefaultRequestHeaders.AcceptLanguage.Add(new(cultureInfo.Name));
        }

        public void UseCurrentCulture() => RequestLanguage = CultureInfo.CurrentCulture;

        internal async IAsyncEnumerable<TTarget> ToAsyncEnumerable<TPage, TRawResult, TTarget>(
            Func<CancellationToken, Task<TPage>> task,
            [EnumeratorCancellation] CancellationToken cancellation = default)
            where TPage : class, IHasNextPage<TRawResult>
            where TTarget : IConstructible<TTarget, TRawResult>
        {
            var response = await task(cancellation).ConfigureAwait(false);

            while (response is not null)
            {
                foreach (var r in response.Items)
                    yield return TTarget.Construct(this, r);

                response = await Api.GetNextPageAsync(response, cancellation).ConfigureAwait(false);
            }
        }

        public IAsyncEnumerable<Illust> GetMyFollowingIllustsAsync(Visibility visibility = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return ToAsyncEnumerable<IllustList, IllustDetail, Illust>(
                c => Api.GetIllustFollowAsync(restrict: visibility, cancellationToken: c),
                cancellation);
        }

        public IAsyncEnumerable<Illust> GetMyBookmarksAsync(Visibility visibility = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return ToAsyncEnumerable<IllustList, IllustDetail, Illust>(
                c => Api.GetUserBookmarkIllustsAsync(userId: CurrentUserId, restrict: visibility, cancellationToken: c),
                cancellation);
        }

        public async Task<UserDetailInfo> GetUserDetailAsync(int userId,
            CancellationToken cancellation = default)
        {
            var response = await Api.GetUserDetailAsync(
                userId,
                cancellationToken: cancellation).ConfigureAwait(false);

            return new UserDetailInfo(this, response);
        }

        public async Task<Illust> GetIllustDetailAsync(int illustId,
            CancellationToken cancellation = default)
        {
            var response = await Api.GetIllustDetailAsync(
                illustId,
                cancellationToken: cancellation).ConfigureAwait(false);

            return new Illust(this, response.Illust);
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetMyFollowingUsersAsync(
            Visibility visibility = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return ToAsyncEnumerable<UsersList, UserPreview, UserInfoWithPreview>(
                c => Api.GetUserFollowingsAsync(
                    userId: CurrentUserId,
                    restrict: visibility,
                    cancellationToken: c), cancellation);
        }

        public IAsyncEnumerable<UserInfoWithPreview> SearchUsersAsync(
            string word,
            CancellationToken cancellation = default)
        {
            return ToAsyncEnumerable<UsersList, UserPreview, UserInfoWithPreview>(
                c => Api.SearchUsersAsync(
                    word: word,
                    cancellationToken: c), cancellation);
        }

        public IAsyncEnumerable<Illust> SearchIllustsAsync(
            string word,
            IllustSearchTarget searchTarget = IllustSearchTarget.PartialTag,
            IllustFilterOptions? options = null,
            CancellationToken cancellation = default)
        {
            return ToAsyncEnumerable<SearchIllustResult, IllustDetail, Illust>(
                c => Api.SearchIllustsAsync(
                    word: word,
                    searchTarget: searchTarget,
                    sort: options?.SortMode ?? IllustSortMode.Latest,
                    maxBookmarkCount: options?.MaxBookmarkCount,
                    minBookmarkCount: options?.MinBookmarkCount,
                    startDate: options?.StartDate,
                    endDate: options?.EndDate,
                    cancellationToken: c), cancellation);
        }

        public IAsyncEnumerable<Illust> GetIllustRankingAsync(
            IllustRankingMode rankingMode = IllustRankingMode.Day,
            DateOnly? date = null,
            CancellationToken cancellation = default)
        {
            return ToAsyncEnumerable<IllustList, IllustDetail, Illust>(
                c => Api.GetIllustRankingAsync(
                    rankingMode,
                    date,
                    cancellationToken: cancellation), cancellation);
        }

        public async Task FollowUserAsync(
            UserInfo userInfo,
            Visibility visibility = Visibility.Public,
            CancellationToken cancellation = default)
        {
            await Api.AddUserFollowAsync(
                userInfo.Id,
                visibility,
                cancellation).ConfigureAwait(false);
        }

        public async Task UnfollowUserAsync(
            UserInfo userInfo,
            CancellationToken cancellation = default)
        {
            await Api.DeleteUserFollowAsync(
                userInfo.Id,
                cancellation).ConfigureAwait(false);
        }

        public async Task AddIllustBookmarkAsync(
            Illust illust,
            Visibility visibility = Visibility.Public,
            IEnumerable<string>? tags = null,
            CancellationToken cancellation = default)
        {
            await Api.AddIllustBookmarkAsync(
                illust.Id,
                visibility,
                tags,
                cancellation).ConfigureAwait(false);
        }

        public async Task DeleteIllustBookmarkAsync(
            Illust illust,
            CancellationToken cancellation = default)
        {
            await Api.DeleteIllustBookmarkAsync(
                illust.Id,
                cancellation).ConfigureAwait(false);
        }

        public async Task<IllustSeries> GetIllustSeriesAsync(
            int illustSeriesId,
            CancellationToken cancellation = default)
        {
            var response = await Api.GetIllustSeriesAsync(
                illustSeriesId,
                cancellation).ConfigureAwait(false);

            return new(this, response.IllustSeriesDetail);
        }

        public async Task<Novel> GetNovelAsync(
            int novelId,
            CancellationToken cancellation = default)
        {
            var response = await Api.GetNovelDetailAsync(
                novelId,
                cancellation).ConfigureAwait(false);

            return new(this, response.Novel);
        }
    }
}
