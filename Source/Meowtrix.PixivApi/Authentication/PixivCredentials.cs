using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Meowtrix.PixivApi.Authentication
{
    public record struct AccessToken(string Token, DateTimeOffset ValidUntil);

    public abstract class PixivCredentials
    {
        public abstract Task<AccessToken> GetAccessTokenAsync(
            HttpMessageInvoker httpMessageInvoker,
            TimeProvider? timeProvider = null,
            CancellationToken cancellationToken = default);
    }
}
