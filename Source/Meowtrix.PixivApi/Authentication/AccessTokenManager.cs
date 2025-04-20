using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Meowtrix.PixivApi.Authentication;

public class AccessTokenManager(
    PixivAuthenticationResult? initialAuthResult,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    private PixivAuthenticationResult? _authResult = initialAuthResult;

    public TimeSpan RefreshThreshold { get; set; } = TimeSpan.FromMinutes(10);

    public void Authenticate(PixivAuthenticationResult authResult) => _authResult = authResult;

    public bool IsAuthenticated => _authResult != null;

    public async ValueTask<string> GetAccessTokenAsync(HttpMessageHandler handler, CancellationToken cancellationToken = default)
    {
        var authResult = _authResult ?? throw new InvalidOperationException("No authentication has been performed.");

        if (authResult.ValidUntil - _timeProvider.GetUtcNow() > RefreshThreshold)
            return authResult.AccessToken;

        _authResult = authResult = await PixivAuthentication.AuthWithRefreshTokenAsync(
            new HttpMessageInvoker(handler, disposeHandler: false), authResult.RefreshToken, _timeProvider, cancellationToken)
            .ConfigureAwait(false);
        return authResult.AccessToken;
    }
}
