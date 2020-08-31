using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
        private PixivApiClient _api;

        #region Construction and disposal
        public PixivClient(bool useDefaultProxy = true)
            => _api = new PixivApiClient(useDefaultProxy);

        public PixivClient(IWebProxy? proxy)
        {
            _api = proxy is null
                ? new PixivApiClient(false)
                : new PixivApiClient(proxy);
        }

        public PixivClient(HttpMessageHandler handler)
            => _api = new PixivApiClient(handler);

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
            _api.Dispose();
            _api = api;
        }

        public void Dispose()
        {
            _api.Dispose();
            _authLock.Dispose();
        }
        #endregion

        #region Lock and token refresh
        private readonly ReaderWriterLockSlim _authLock = new ReaderWriterLockSlim();
        private DateTimeOffset _authValidateUntil;

        private string? _accessToken;
        private string? _refreshToken;

#if NET5_0
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(_accessToken), nameof(_refreshToken))]
#endif
        public bool IsLogin => _accessToken is not null && _refreshToken is not null;

        public async Task LoginAsync(string username, string password)
        {
            try
            {
                _authLock.EnterWriteLock();

                var (time, response) = await _api.AuthAsync(username, password).ConfigureAwait(false);
                _accessToken = response.AccessToken;
                _refreshToken = response.RefreshToken;
                _authValidateUntil = time.AddSeconds(response.ExpiresIn);
            }
            finally
            {
                _authLock.ExitWriteLock();
            }
        }

        public async Task LoginAsync(string refreshToken)
        {
            try
            {
                _authLock.EnterWriteLock();

                var (time, response) = await _api.AuthAsync(refreshToken).ConfigureAwait(false);
                _accessToken = response.AccessToken;
                _refreshToken = response.RefreshToken;
                _authValidateUntil = time.AddSeconds(response.ExpiresIn);
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
                            var (time, response) = await _api.AuthAsync(_refreshToken).ConfigureAwait(false);
                            _accessToken = response.AccessToken;
                            _refreshToken = response.RefreshToken;
                            _authValidateUntil = time.AddSeconds(response.ExpiresIn);

                            return _accessToken;
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
        #endregion
    }
}
