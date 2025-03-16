using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Meowtrix.PixivApi.Authentication
{
    public record struct AccessToken(string Token, DateTimeOffset ValidUntil);

    /// <summary>
    /// A base class for authentication credentials. In case of authentication failure,
    /// you can replace with your own implementation.
    /// </summary>
    public abstract class PixivCredentials
    {
        public abstract Task<AccessToken> GetAccessTokenAsync(
            HttpMessageInvoker httpMessageInvoker,
            TimeProvider? timeProvider = null,
            CancellationToken cancellationToken = default);
    }
}
