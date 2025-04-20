using System;
using System.Buffers.Text;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Authentication;

/// <summary>
/// Provide OAuth-based authentication for Pixiv.
/// </summary>
public static class PixivAuthentication
{
    private const string ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
    private const string ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
    private const string UserAgent = "PixivAndroidApp/5.0.166 (Android 12.0)";
    private const string AuthUrl = "https://oauth.secure.pixiv.net/auth/token";

#if NET
    private static ReadOnlySpan<byte> HashSecret => "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c"u8;
#else
    private const string HashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c";
#endif

    private static (string Time, string ClientHash) BuildClientTimeHeader(DateTimeOffset timestamp)
    {
        string requestTime = timestamp.ToString(@"yyyy-MM-dd\THH\:mm\:ssK");
#if NET
        Span<byte> clientHash = stackalloc byte[MD5.HashSizeInBytes];
        MD5.HashData([.. Encoding.UTF8.GetBytes(requestTime), .. HashSecret], clientHash);
#else
        using var md5 = MD5.Create();
        byte[] clientHash = md5.ComputeHash(Encoding.UTF8.GetBytes(requestTime + HashSecret));
#endif

#if NET9_0_OR_GREATER
        return (requestTime, Convert.ToHexStringLower(clientHash));
#elif NET
        return (requestTime, Convert.ToHexString(clientHash).ToLowerInvariant());
#else
        return (requestTime, BitConverter.ToString(clientHash).Replace("-", "").ToLowerInvariant());
#endif
    }

    /// <summary>
    /// Get authentication infomation with refresh token.
    /// </summary>
    /// <param name="httpMessageInvoker">The http client to send authentication request.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="timeProvider">The time provider to verify client time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result.</returns>
    /// <exception cref="AuthenticationFailedException">When authentication failed.</exception>
    public static async Task<PixivAuthenticationResult> AuthWithRefreshTokenAsync(
        HttpMessageInvoker httpMessageInvoker,
        string refreshToken,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        timeProvider ??= TimeProvider.System;
        (string requestTime, string clientHash) = BuildClientTimeHeader(timeProvider.GetUtcNow());

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
                { "X-Client-Hash", clientHash },
            },
#if NET
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
#endif
        };

        var response = await httpMessageInvoker.RequestRefreshTokenAsync(request, cancellationToken).ConfigureAwait(false);
        return response.CheckAuthenticationResult(timeProvider);
    }

    /// <summary>
    /// Prepare the parameters for doing browser-based login.
    /// </summary>
    /// <returns>The parameters for login.</returns>
    /// <remarks>
    /// To complete the login process, navigate to <c>LoginUrl</c> in browser, listen to <c>pixiv://....?code=....</c> ,
    /// get authentication code from the <c>code</c> query parameter, and invoke <see cref="CompleteWebLoginAsync"/>.
    /// </remarks>
    public static (string CodeVerify, string LoginUrl) PrepareWebLogin()
    {
#if NET
        Span<byte> bytes = stackalloc byte[36];
        RandomNumberGenerator.Fill(bytes);

        string codeVerifyString = Convert.ToBase64String(bytes);

        Span<byte> codeVerify = stackalloc byte[48];
        Encoding.UTF8.GetBytes(codeVerifyString, codeVerify);

        Span<byte> sha = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(codeVerify, sha);
#else
        using var rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[36];
        rng.GetBytes(bytes);

        string codeVerifyString = Convert.ToBase64String(bytes);
        byte[] codeVerify = Encoding.UTF8.GetBytes(codeVerifyString);

        using var sha256 = SHA256.Create();
        byte[] sha = sha256.ComputeHash(codeVerify);

#endif

#if NET9_0_OR_GREATER
        Span<char> urlSafeCodeChallenge = stackalloc char[43];
        Base64Url.EncodeToChars(sha, urlSafeCodeChallenge);
#else
        string urlSafeCodeChallenge = Convert.ToBase64String(sha)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
#endif

        string loginUrl = $"https://app-api.pixiv.net/web/v1/login?code_challenge={urlSafeCodeChallenge}&code_challenge_method=S256&client=pixiv-android";

        return (codeVerifyString, loginUrl);
    }

    /// <summary>
    /// Complete browser-based authentication.
    /// </summary>
    /// <param name="httpMessageInvoker">The http client to send authentication request.</param>
    /// <param name="authorizationCode">The authentication code acquired from browser.</param>
    /// <param name="codeVerify">The verification code returned from <see cref="PrepareWebLogin"/>.</param>
    /// <param name="timeProvider">The time provider to verify client time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result.</returns>
    /// <exception cref="AuthenticationFailedException">When authentication failed.</exception>
    public static async Task<PixivAuthenticationResult> CompleteWebLoginAsync(
        HttpMessageInvoker httpMessageInvoker,
        string authorizationCode,
        string codeVerify,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        timeProvider ??= TimeProvider.System;
        (string requestTime, string clientHash) = BuildClientTimeHeader(timeProvider.GetUtcNow());

        using var request = new AuthorizationCodeTokenRequest
        {
            Address = AuthUrl,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            Code = authorizationCode,
            RedirectUri = "https://app-api.pixiv.net/web/v1/users/auth/pixiv/callback",
            CodeVerifier = codeVerify,
            Headers =
            {
                { "User-Agent", UserAgent },
                { "X-Client-Time", requestTime },
                { "X-Client-Hash", clientHash },
            },
#if NET
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
#endif
        };

        var response = await httpMessageInvoker.RequestAuthorizationCodeTokenAsync(request, cancellationToken).ConfigureAwait(false);
        return response.CheckAuthenticationResult(timeProvider);
    }

    /// <summary>
    /// Gets authentication result from <see cref="IdentityModel"/> response.
    /// </summary>
    /// <param name="response">A response message from <see cref="IdentityModel"/>.</param>
    /// <param name="timeProvider">The time provide to calculate <see cref="PixivAuthenticationResult.ValidUntil"/>.</param>
    /// <returns>The authentication result.</returns>
    /// <exception cref="AuthenticationFailedException">When authentication failed.</exception>
    public static PixivAuthenticationResult CheckAuthenticationResult(this TokenResponse response, TimeProvider? timeProvider = null)
    {
        if (response.IsError)
        {
            throw new AuthenticationFailedException(response.Error, response.Exception);
        }

        if (response.AccessToken is null || response.RefreshToken is null)
        {
            throw new AuthenticationFailedException("No access token is provided.");
        }

        var validUntil = (timeProvider ?? TimeProvider.System).GetUtcNow().AddSeconds(response.ExpiresIn);
        var userInfo = response.Json?.GetProperty("user"u8).Deserialize(PixivJsonContext.Default.AuthUser)
            ?? throw new AuthenticationFailedException("No user info is provided.");
        return new(response.AccessToken, response.RefreshToken, validUntil, userInfo);
    }
}

public class AuthenticationFailedException(string? message, Exception? innerException = null) : Exception(message, innerException)
{
}

public record class PixivAuthenticationResult(string AccessToken, string RefreshToken, DateTimeOffset ValidUntil, AuthUser UserInfo);
