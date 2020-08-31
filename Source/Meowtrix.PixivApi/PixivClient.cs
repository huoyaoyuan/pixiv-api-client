using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            Api.Dispose();
            Api = api;
        }

        public void Dispose()
        {
            Api.Dispose();
            _authLock.Dispose();
        }
        #endregion

        #region Lock and token refresh
        private readonly ReaderWriterLockSlim _authLock = new ReaderWriterLockSlim();
        private DateTimeOffset _authValidateUntil;

        private string? _accessToken;
        private string? _refreshToken;

#if NET5_0
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true,
            nameof(_accessToken),
            nameof(_refreshToken),
            nameof(CurrentUser))]
#endif
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

        public async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                _authLock.EnterWriteLock();

                var (time, response) = await Api.AuthAsync(username, password).ConfigureAwait(false);
                SetLogin(time, response);

                return response.RefreshToken;
            }
            finally
            {
                _authLock.ExitWriteLock();
            }
        }

        public async Task<string> LoginAsync(string refreshToken)
        {
            try
            {
                _authLock.EnterWriteLock();

                var (time, response) = await Api.AuthAsync(refreshToken).ConfigureAwait(false);
                SetLogin(time, response);

                return response.RefreshToken;
            }
            finally
            {
                _authLock.ExitWriteLock();
            }
        }

        private async ValueTask<string> CheckValidAccessToken(int epsilonTimeSeconds = 60)
        {
            if (!IsLogin)
            {
                throw new InvalidOperationException("The client isn't logged in.");
            }

            try
            {
                _authLock.EnterUpgradeableReadLock();

                if ((_authValidateUntil - DateTimeOffset.Now).TotalSeconds < epsilonTimeSeconds)
                {
                    try
                    {
                        _authLock.EnterWriteLock();

                        if ((_authValidateUntil - DateTimeOffset.Now).TotalSeconds < epsilonTimeSeconds)
                        {
                            var (time, response) = await Api.AuthAsync(_refreshToken).ConfigureAwait(false);
                            SetLogin(time, response);

                            return response.AccessToken;
                        }
                    }
                    finally
                    {
                        _authLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _authLock.ExitUpgradeableReadLock();
            }

            return _accessToken;
        }

        private void SetLogin(DateTimeOffset authTime, AuthResponse response)
        {
            _accessToken = response.AccessToken;
            _refreshToken = response.RefreshToken;
            _authValidateUntil = authTime.AddSeconds(response.ExpiresIn);

            CurrentUser = new LoginUser(this, response.User);
        }
        #endregion

        public LoginUser? CurrentUser { get; private set; }

#if NET5_0
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(CurrentUser))]
#endif
        public int CurrentUserId => CurrentUser?.Id ?? throw new InvalidOperationException("No user login.");
    }
}
