namespace Meowtrix.PixivApiFS

open System
open System.Buffers.Text
open System.Net.Http
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Threading.Tasks
open IdentityModel.Client

type PixivAuthenticationResult =
    { accessToken: string;
      refreshToken: string; 
      validUntil: DateTimeOffset;
      userInfo: AuthUser }

type AuthenticationFailedException(message: string | null, innerException: exn | null) =
    inherit Exception(message, innerException)

module PixivAuthentication =
    [<Literal>]
    let private ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT"
    [<Literal>]
    let private ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj"
    [<Literal>]
    let private UserAgent = "PixivAndroidApp/5.0.166 (Android 12.0)"
    [<Literal>]
    let private HashSecret = "28c1fdd170a5204386cb1313c7077b34f83e4aaf4aa829ce78c231e05b0bae2c"
    [<Literal>]
    let private AuthUrl = "https://oauth.secure.pixiv.net/auth/token"

    let private BuildClientTimeHeader (timestamp: DateTimeOffset) =
        let requestTime = timestamp.ToString @"yyyy-MM-dd\THH\:mm\:ssK"
        let clientHash =
            requestTime + HashSecret
            |> Encoding.UTF8.GetBytes
            |> Convert.ToHexStringLower
        (requestTime, clientHash)
        
    let CheckAuthenticationResult (response: TokenResponse) (timeProvider: TimeProvider) =
        if response.IsError
        then AuthenticationFailedException(response.Error, response.Exception) |> raise
        else
            match response.AccessToken, response.RefreshToken with
            | (null, _) | (_, null) -> raise(AuthenticationFailedException("No access token is provided.", null))
            | (accessToken, refreshToken) ->
                let validUntil = timeProvider.GetUtcNow().AddSeconds(response.ExpiresIn)
                let userInfo =
                    match response.Json with
                    | NonNullV jsonElement -> jsonElement.GetProperty("user") |> JsonSerializer.Deserialize<AuthUser>
                    | _ -> raise(AuthenticationFailedException("No user info is provided.", null))
                { accessToken = accessToken; refreshToken = refreshToken; validUntil = validUntil; userInfo = nonNull userInfo }
                
    let AuthWithRefreshTokenAsync (httpMessageInvoker: HttpMessageInvoker) refreshToken (timeProvider: TimeProvider) =
        async {
            let (requestTime, clientHash) = timeProvider.GetUtcNow() |> BuildClientTimeHeader
            use request = new RefreshTokenRequest(
                Address = AuthUrl,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                RefreshToken = refreshToken
            )
            request.Headers.Add("UserAgent",UserAgent)
            request.Headers.Add("X-Client-Time", requestTime)
            request.Headers.Add("X-Client-Hash", clientHash)
            let! response =
                httpMessageInvoker.RequestRefreshTokenAsync(request, Async.DefaultCancellationToken)
                |> Async.AwaitTask

            return CheckAuthenticationResult response timeProvider
        }

    let PrepareWebLogin() =
        let codeVerify =
            RandomNumberGenerator.GetBytes 36
            |> Convert.ToBase64String
        let codeChallenge =
            codeVerify
            |> Encoding.UTF8.GetBytes
            |> SHA256.HashData
            |> Base64Url.EncodeToString
        let loginUrl = $"https://app-api.pixiv.net/web/v1/login?code_challenge={codeChallenge}&code_challenge_method=S256&client=pixiv-android"
        (codeVerify, loginUrl)

    let CompleteWebLoginAsync (httpMessageInvoker: HttpMessageInvoker) authroizationCode codeVerify (timeProvider: TimeProvider) =
        async {
            let (requestTime, clientHash) = timeProvider.GetUtcNow() |> BuildClientTimeHeader
            use request = new AuthorizationCodeTokenRequest(
                Address = AuthUrl,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Code = authroizationCode,
                RedirectUri = "https://app-api.pixiv.net/web/v1/users/auth/pixiv/callback",
                CodeVerifier = codeVerify
            )
            request.Headers.Add("UserAgent",UserAgent)
            request.Headers.Add("X-Client-Time", requestTime)
            request.Headers.Add("X-Client-Hash", clientHash)
            let! response =
                httpMessageInvoker.RequestAuthorizationCodeTokenAsync(request, Async.DefaultCancellationToken)
                |> Async.AwaitTask

            return CheckAuthenticationResult response timeProvider
        }

    type AccessTokenManager(initialAuthResult: PixivAuthenticationResult, ?timeProvider: TimeProvider) =
        let timeProvider = defaultArg timeProvider TimeProvider.System

        let mutable authResult = initialAuthResult
        member val RefreshThreshold = TimeSpan.FromMinutes(10L) with get, set

        member this.GetAccessTokenAsync(handler: HttpMessageHandler) =
            async {
                let authResultSnap = authResult
                if authResultSnap.validUntil - timeProvider.GetUtcNow() > this.RefreshThreshold
                then return authResultSnap.accessToken
                else
                    let httpMessageInvoker = new HttpMessageInvoker(handler, disposeHandler=false)
                    let! newAuthResult = AuthWithRefreshTokenAsync httpMessageInvoker authResultSnap.refreshToken timeProvider
                    authResult <- newAuthResult
                    return newAuthResult.accessToken
            }
