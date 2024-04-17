using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Meowtrix.PixivApi.Authentication
{
    public sealed class RefreshTokenCredentials(string refreshToken) : PixivCredentials
    {
        private const string ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
        private const string ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
        private const string UserAgent = "PixivAndroidApp/5.0.64 (Android 6.0)";
        private const string AuthUrl = "https://oauth.secure.pixiv.net/auth/token";

        public override async Task<AccessToken> GetAccessTokenAsync(
            HttpMessageInvoker httpMessageInvoker,
            TimeProvider? timeProvider = null,
            CancellationToken cancellationToken = default)
        {
            timeProvider ??= TimeProvider.System;

            using var request = new RefreshTokenRequest
            {
                Address = AuthUrl,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                RefreshToken = refreshToken,
                Headers =
                {
                    { "User-Agent", UserAgent }
                }
            };

            var response = await httpMessageInvoker.RequestRefreshTokenAsync(
                request, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsError || response.AccessToken is null)
            {
                throw new AuthenticationFailedException(response.ErrorDescription, response.Exception);
            }

            var responseDate = response.HttpResponse?.Headers.Date ?? timeProvider.GetUtcNow();
            var validUntil = responseDate.AddSeconds(response.ExpiresIn);

            refreshToken = response.RefreshToken ?? refreshToken;
            return new(response.AccessToken, validUntil);
        }
    }
}
