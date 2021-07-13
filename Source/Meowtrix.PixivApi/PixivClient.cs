using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        internal PixivApiClient Api { get; private set; }

        #region Construction and disposal
        public PixivClient(bool useDefaultProxy = true)
            => Api = new PixivApiClient(useDefaultProxy);

        public PixivClient(IWebProxy? proxy)
        {
            Api = proxy is null
                ? new PixivApiClient(false)
                : new PixivApiClient(proxy);
        }

        public PixivClient(HttpMessageHandler handler)
            => Api = new PixivApiClient(handler);

        public void SetProxy(IWebProxy? proxy)
        {
            ChangeApiClient(proxy is null
                ? new PixivApiClient(false)
                : new PixivApiClient(proxy));
        }

        public void SetDefaultProxy()
            => ChangeApiClient(new PixivApiClient(true));

        public void SetHandler(HttpMessageHandler handler)
            => ChangeApiClient(new PixivApiClient(handler));

        private void ChangeApiClient(PixivApiClient api)
        {
            SetRequestHeader(api, RequestLanguage);
            Api.Dispose();
            Api = api;
        }

        public void Dispose()
        {
            Api.Dispose();
            _semaphore.Dispose();
        }
        #endregion

        #region Lock and token refresh
        private readonly SemaphoreSlim _semaphore = new(1);
        private DateTimeOffset _authValidateUntil;

        private string? _accessToken;
        private string? _refreshToken;

        [MemberNotNullWhen(true,
            nameof(_accessToken),
            nameof(_refreshToken),
            nameof(CurrentUser))]
        public bool IsLogin
        {
            get
            {
                if (_accessToken is not null)
                {
                    Debug.Assert(CurrentUser is not null);
                    Debug.Assert(_refreshToken is not null);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Perform login with OAuth PKCE.
        /// </summary>
        /// <param name="requestFunc">Accesses the url parameter in browser,
        /// listens and returns for pixiv:// request.</param>
        /// <returns>The refresh token.</returns>
        public async Task<string> LoginAsync(Func<string, Task<Uri>> requestFunc)
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                var (codeVerify, loginUrl) = Api.BeginAuth();
                var uri = await requestFunc(loginUrl).ConfigureAwait(false);

                if (uri.Scheme != "pixiv")
                    throw new InvalidOperationException("Bad login request.");
                var query = HttpUtility.ParseQueryString(uri.Query);
                string code = query["code"] ?? throw new InvalidOperationException("The login request doesn't contain code.");
                var (time, response) = await Api.CompleteAuthAsync(code, codeVerify).ConfigureAwait(false);
                SetLogin(time, response);

                return response.RefreshToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        [Obsolete("Login with username and password has been abandoned by Pixiv.")]
        public async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                var (time, response) = await Api.AuthAsync(username, password).ConfigureAwait(false);
                SetLogin(time, response);

                return response.RefreshToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string> LoginAsync(string refreshToken)
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                var (time, response) = await Api.AuthAsync(refreshToken).ConfigureAwait(false);
                SetLogin(time, response);

                return response.RefreshToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }



        [MemberNotNull(nameof(CurrentUser))]
        private async ValueTask<string> CheckTokenAsyncCore(int epsilonTimeSeconds = 60)
        {
            if (!IsLogin)
            {
                throw new InvalidOperationException("The client isn't logged in.");
            }

            if ((_authValidateUntil - DateTimeOffset.Now).TotalSeconds < epsilonTimeSeconds)
            {
                try
                {
                    await _semaphore.WaitAsync().ConfigureAwait(false);

                    if ((_authValidateUntil - DateTimeOffset.Now).TotalSeconds < epsilonTimeSeconds)
                    {
                        var (time, response) = await Api.AuthAsync(_refreshToken).ConfigureAwait(false);
                        SetLogin(time, response);

                        return response.AccessToken;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return _accessToken;
        }

        internal ConfiguredValueTaskAwaitable<string> CheckTokenAsync(int epsilonTimeSeconds = 60)
            => CheckTokenAsyncCore(epsilonTimeSeconds).ConfigureAwait(false);

        private void SetLogin(DateTimeOffset authTime, AuthResponse response)
        {
            _accessToken = response.AccessToken;
            _refreshToken = response.RefreshToken;
            _authValidateUntil = authTime.AddSeconds(response.ExpiresIn);

            CurrentUser = new LoginUser(this, response.User);
        }
        #endregion

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
                    SetRequestHeader(Api, value);
                }
            }
        }

        private static void SetRequestHeader(PixivApiClient apiClient, CultureInfo? cultureInfo)
        {
            apiClient.DefaultRequestHeaders.AcceptLanguage.Clear();
            if (!string.IsNullOrEmpty(cultureInfo?.Name))
                apiClient.DefaultRequestHeaders.AcceptLanguage.Add(new(cultureInfo.Name));
        }

        public void UseCurrentCulture() => RequestLanguage = CultureInfo.CurrentCulture;

        internal async IAsyncEnumerable<Illust> ToIllustAsyncEnumerable<T>(
            Func<string, CancellationToken, Task<T>> task,
            [EnumeratorCancellation] CancellationToken cancellation = default)
            where T : class, IHasNextPage<IllustDetail>
        {
            var response = await task(await CheckTokenAsync(), cancellation).ConfigureAwait(false);

            while (response is not null)
            {
                foreach (var r in response.Items)
                    yield return new Illust(this, r);

                response = await Api.GetNextPageAsync(await CheckTokenAsync(),
                    response,
                    cancellation: cancellation).ConfigureAwait(false);
            }
        }

        internal async IAsyncEnumerable<UserInfoWithPreview> ToUserAsyncEnumerable<T>(
            Func<string, CancellationToken, Task<T>> task,
            [EnumeratorCancellation] CancellationToken cancellation = default)
            where T : class, IHasNextPage<UserPreview>
        {
            var response = await task(await CheckTokenAsync(), cancellation).ConfigureAwait(false);

            while (response is not null)
            {
                foreach (var r in response.Items)
                    yield return new UserInfoWithPreview(this, r);

                response = await Api.GetNextPageAsync(await CheckTokenAsync(),
                    response,
                    cancellation: cancellation).ConfigureAwait(false);
            }
        }

        public IAsyncEnumerable<Illust> GetMyFollowingIllustsAsync(Visibility visibility = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return ToIllustAsyncEnumerable(async (auth, c)
                => await Api.GetIllustFollowAsync(authToken: auth,
                restrict: visibility,
                cancellation: c).ConfigureAwait(false),
                cancellation);
        }

        public IAsyncEnumerable<Illust> GetMyBookmarksAsync(Visibility visibility = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return ToIllustAsyncEnumerable(async (auth, c)
                => await Api.GetUserBookmarkIllustsAsync(authToken: auth, userId: CurrentUserId,
                restrict: visibility,
                cancellation: c).ConfigureAwait(false),
                cancellation);
        }

        public async Task<UserDetailInfo> GetUserDetailAsync(int userId,
            CancellationToken cancellation = default)
        {
            var response = await Api.GetUserDetailAsync(await CheckTokenAsync(),
                userId,
                cancellation: cancellation).ConfigureAwait(false);

            return new UserDetailInfo(this, response);
        }

        public async Task<Illust> GetIllustDetailAsync(int illustId,
            CancellationToken cancellation = default)
        {
            var response = await Api.GetIllustDetailAsync(await CheckTokenAsync(),
                illustId,
                cancellation: cancellation).ConfigureAwait(false);

            return new Illust(this, response.Illust);
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetMyFollowingUsersAsync(
            Visibility visibility = Visibility.Public,
            CancellationToken cancellation = default)
        {
            return ToUserAsyncEnumerable(async (auth, c)
                => await Api.GetUserFollowingsAsync(
                    authToken: auth,
                    userId: CurrentUserId,
                    restrict: visibility,
                    cancellation: c).ConfigureAwait(false), cancellation);
        }

        public IAsyncEnumerable<UserInfoWithPreview> SearchUsersAsync(
            string word,
            CancellationToken cancellation = default)
        {
            return ToUserAsyncEnumerable(async (auth, c)
                => await Api.SearchUsersAsync(
                    authToken: auth,
                    word: word,
                    cancellation: c).ConfigureAwait(false), cancellation);
        }

        public IAsyncEnumerable<Illust> SearchIllustsAsync(
            string word,
            IllustSearchTarget searchTarget = IllustSearchTarget.PartialTag,
            IllustFilterOptions? options = null,
            CancellationToken cancellation = default)
        {
            return ToIllustAsyncEnumerable(async (auth, c)
                => await Api.SearchIllustsAsync(
                    authToken: auth, word: word,
                    searchTarget: searchTarget,
                    sort: options?.SortMode ?? IllustSortMode.Latest,
                    maxBookmarkCount: options?.MaxBookmarkCount,
                    minBookmarkCount: options?.MinBookmarkCount,
                    startDate: options?.StartDate,
                    endDate: options?.EndDate,
                    cancellation: c).ConfigureAwait(false), cancellation);
        }

        public IAsyncEnumerable<Illust> GetIllustRankingAsync(
            IllustRankingMode rankingMode = IllustRankingMode.Day,
            DateTime? date = null,
            CancellationToken cancellation = default)
        {
            return ToIllustAsyncEnumerable(async (auth, c)
                => await Api.GetIllustRankingAsync(
                    authToken: auth, mode: rankingMode,
                    date: date,
                    cancellation: cancellation).ConfigureAwait(false), cancellation);
        }
    }
}
