using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Meowtrix.PixivApi.Authentication;

internal class PixivAccessTokenHandler(HttpMessageHandler innerHandler, AccessTokenManager tokenManager) : DelegatingHandler(innerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBearerToken(await tokenManager.GetAccessTokenAsync(InnerHandler!, cancellationToken).ConfigureAwait(false));
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

#if NET
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBearerToken(tokenManager.GetAccessTokenAsync(InnerHandler!, cancellationToken).Preserve().Result);
        return base.Send(request, cancellationToken);
    }
#endif
}
