using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Authentication;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi;

public class PixivApiClient2 : HttpClient
{
    private const string BaseUrl = "https://app-api.pixiv.net/";

    public PixivApiClient2(AccessTokenManager accessTokenManager, HttpMessageHandler? httpMessageHandler = null, bool disposeHandler = true)
        : base(new PixivAccessTokenHandler(accessTokenManager, httpMessageHandler ?? new HttpClientHandler()), disposeHandler)
    {
        BaseAddress = new(BaseUrl);
        DefaultRequestHeaders.Add("User-Agent", "PixivAndroidApp/5.0.166 (Android 12.0)");
#if NET
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
#endif
    }

    private async Task<T> InvokeGetAsync<T>(string relativeUri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        var result = await this.GetFromJsonAsync(new Uri(relativeUri, UriKind.RelativeOrAbsolute), jsonTypeInfo, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("The api returns top-level null.");
    }

    public Task<UserDetail> GetUserDetailAsync(
        int userId,
        CancellationToken cancellation = default)
    {
        return InvokeGetAsync(
            $"/v1/user/detail?user_id={userId}",
            PixivJsonContext.Default.UserDetail,
            cancellation)!;
    }
}
