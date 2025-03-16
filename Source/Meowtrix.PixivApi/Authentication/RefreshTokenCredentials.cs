using System.Net.Http;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using IdentityModel.Client;

namespace Meowtrix.PixivApi.Authentication
{
    public class RefreshTokenCredentials(string refreshToken) : PixivCredentials
    {
        private const string ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
        private const string ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
        private const string UserAgent = "PixivAndroidApp/5.0.166 (Android 12.0)";
        private const string HashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";
        private const string AuthUrl = "https://oauth.secure.pixiv.net/auth/token";

        public override async Task<AccessToken> GetAccessTokenAsync(
            HttpMessageInvoker httpMessageInvoker,
            TimeProvider? timeProvider = null,
            CancellationToken cancellationToken = default)
        {
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


            timeProvider ??= TimeProvider.System;
            string requestTime = timeProvider.GetUtcNow().ToString(@"yyyy-MM-dd\THH\:mm\:ssK");

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

            var response = await httpMessageInvoker.RequestRefreshTokenAsync(
                request, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsError || response.AccessToken is null)
            {
                throw new InvalidOperationException(response.ErrorDescription, response.Exception);
            }

            var responseDate = response.HttpResponse?.Headers.Date ?? timeProvider.GetUtcNow();
            var validUntil = responseDate.AddSeconds(response.ExpiresIn);

            refreshToken = response.RefreshToken ?? refreshToken;
            return new(response.AccessToken, validUntil);
        }
    }
}
