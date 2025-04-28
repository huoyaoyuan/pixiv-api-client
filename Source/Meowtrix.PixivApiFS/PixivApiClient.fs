namespace Meowtrix.PixivApiFS

open System
open System.Net.Http
open System.Net.Http.Json
open FsHttp
open Meowtrix.PixivApiFS.PixivAuthentication

[<RequireQualifiedAccess>]
type UserIllustType = illustrations | comics

[<RequireQualifiedAccess>]
type Visibility = ``public`` | ``private``

[<RequireQualifiedAccess>]
type IllustRankingMode =
    | day
    | week
    | month
    | day_male
    | day_female
    | week_original
    | week_rookie
    | day_manga
    | day_r18
    | week_r18
    | day_male_r18
    | day_female_r18

type PixivApiClient(tokenManager: AccessTokenManager, innerHandler: HttpMessageHandler, disposeHandler: bool) =
    [<Literal>]
    let BaseUrl = "https://app-api.pixiv.net/"
    let httpClient = new HttpClient(innerHandler, disposeHandler,
        BaseAddress=Uri(BaseUrl),
        DefaultVersionPolicy=HttpVersionPolicy.RequestVersionOrHigher)

    // FsHttp uses UriBuilder, which doesn't support relative Uri
    let http = http {
        config_useBaseUrl BaseUrl
    }

    interface IDisposable with
        member _.Dispose() = httpClient.Dispose()

    member private _.sendAsync<'a when 'a : not struct and 'a : not null> (req: HeaderContext) =
        async {
            let! token = tokenManager.GetAccessTokenAsync innerHandler
            let! response =
                httpClient.SendAsync(
                    req {
                        AuthorizationBearer token
                    }
                    |> Request.toHttpRequestMessage,
                    Async.DefaultCancellationToken)
                |> Async.AwaitTask
            let content = response.EnsureSuccessStatusCode().Content
            let! result =
                HttpContentJsonExtensions.ReadFromJsonAsync<'a>(content, Async.DefaultCancellationToken)
                |> Async.AwaitTask
            return nonNull result
        }

    member this.GetUserDetailAsync (userId: int) =
        http {
            GET "v1/user/detail"
            query [
                "user_id", userId.ToString()
            ]
        }
        |> this.sendAsync<UserDetail>
        
    member this.GetIllustDetailAsync (illustId: int) =
        http {
            GET "v1/illust/detail"
            query [
                "illust_id", illustId.ToString()
            ]
        }
        |> this.sendAsync<IllustDetailResponse>

    member this.GetUserIllustsAsync (userId: int) (illustType: UserIllustType voption) (offset: int) =
        http {
            GET "v1/user/illusts"
            query [
                "user_id", userId.ToString()
                if illustType.IsSome then "illust_type", illustType.Value.ToString()
                "offset", offset.ToString()
            ]
        }
        |> this.sendAsync<IllustList>

    member this.GetIllustFollowAsync (visibility: Visibility) (offset: int) =
        http {
            GET "v2/illust/follow"
            query [
                "visibility", visibility.ToString()
                "offset", offset.ToString()
            ]
        }
        |> this.sendAsync<IllustList>

    member this.GetIllustRankingAsync (mode: IllustRankingMode, date: DateOnly voption, offset: int) =
        http {
            GET "v1/illust/ranking"
            query [
                "mode", mode.ToString()
                if date.IsSome then "date", date.Value.ToString("yyyy-MM-dd")
                "offset", offset.ToString()
            ]
        }
        |> this.sendAsync<IllustList>

    member this.GetUserNovelsAsync (userId: int) =
        http {
            GET "v1/user/novels"
            query [
                "user_id", userId.ToString()
            ]
        }
        |> this.sendAsync<NovelList>
        
    member this.GetNovelDetailAsync (novelId: int) =
        http {
            GET "v2/novel/detail"
            query [
                "novel_id", novelId.ToString()
            ]
        }
        |> this.sendAsync<NovelDetailResponse>
        
    member this.GetNovelSeriesAsync (seriesId: int) =
        http {
            GET "v2/novel/series"
            query [
                "series_id", seriesId.ToString()
            ]
        }
        |> this.sendAsync<NovelSeries>

    member this.GetNovelHtmlAsync (novelId: int) =
        httpClient.GetStringAsync($"webview/v2/novel?id={novelId}", Async.DefaultCancellationToken)
        |> Async.AwaitTask

    member this.GetImageAsync (uri: Uri) =
        async {
            let! token = tokenManager.GetAccessTokenAsync innerHandler
            let request = 
                http {
                    GET (uri.ToString())
                    AuthorizationBearer token
                    Referer BaseUrl
                }
                |> Request.toHttpRequestMessage
            let! response = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Async.DefaultCancellationToken) |> Async.AwaitTask
            return! response
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync(Async.DefaultCancellationToken)
                |> Async.AwaitTask
        }
