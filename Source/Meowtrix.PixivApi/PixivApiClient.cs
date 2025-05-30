﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Meowtrix.PixivApi.Authentication;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi;

/// <summary>
/// A type to provide direct API call to Pixiv.
/// </summary>
/// <remarks>
/// This type doesn't manage authentication. Access token is provided via <see cref="AccessTokenManager"/>.
/// </remarks>
public class PixivApiClient : IDisposable
{
    private const string BaseUrl = "https://app-api.pixiv.net/";
    private static readonly Uri s_baseUri = new(BaseUrl);

    /// <summary>
    /// The http client for sending requests.
    /// </summary>
    public HttpClient HttpClient { get; }

    /// <summary>
    /// The inner <see cref="HttpMessageHandler"/> that doesn't perform authentication.
    /// </summary>
    public HttpMessageHandler InnerHandler { get; }

    public PixivApiClient(AccessTokenManager accessTokenManager, HttpMessageHandler? innerHandler = null, bool disposeHandler = true)
    {
        InnerHandler = innerHandler ?? new HttpClientHandler();
        HttpClient = new HttpClient(new PixivAccessTokenHandler(accessTokenManager, InnerHandler), disposeHandler)
        {

            BaseAddress = s_baseUri,
            DefaultRequestHeaders =
            {
                { "User-Agent", "PixivAndroidApp/5.0.166 (Android 12.0)" },
                { "Referer", BaseUrl },
            },
#if NET
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
#endif
        };

    }

    public void Dispose() => HttpClient.Dispose();

    private async Task<T> InvokeGetAsync<T>(string relativeUri, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        var result = await HttpClient.GetFromJsonAsync(new Uri(relativeUri, UriKind.RelativeOrAbsolute), jsonTypeInfo, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("The api returns top-level null.");
    }

    private async Task<HttpResponseMessage> InvokePostAsync(string relativeUri, IEnumerable<KeyValuePair<string, string>> formBody, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(relativeUri, UriKind.RelativeOrAbsolute))
        {
            Content = new FormUrlEncodedContent(formBody),
#if NET
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
#endif
        };
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.EnsureSuccessStatusCode();
    }

    private async Task<T> InvokePostAsync<T>(string relativeUri, IEnumerable<KeyValuePair<string, string>> formBody, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(relativeUri, UriKind.RelativeOrAbsolute))
        {
            Content = new FormUrlEncodedContent(formBody),
#if NET
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
#endif
        };
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("The api returns top-level null.");
    }

    public Task<UserDetail> GetUserDetailAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/user/detail?user_id={userId}",
            PixivJsonContext.Default.UserDetail,
            cancellationToken)!;
    }

    public Task<IllustDetailResponse> GetIllustDetailAsync(
        int illustId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/illust/detail?illust_id={illustId}",
            PixivJsonContext.Default.IllustDetailResponse,
            cancellationToken);
    }

    public Task<IllustList> GetUserIllustsAsync(
        int userId,
        UserIllustType? illustType = null,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        string url = $"v1/user/illusts?user_id={userId}&offset={offset}";
        if (illustType is UserIllustType type)
            url += $"&type={type.ToQueryString()}";
        return InvokeGetAsync(
            url,
            PixivJsonContext.Default.IllustList,
            cancellationToken);
    }

    public Task<IllustList> GetUserBookmarkIllustsAsync(
        int userId,
        Visibility restrict = Visibility.Public,
        int? maxBookmarkId = null,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        string url = $"v1/user/bookmarks/illust?user_id={userId}&restrict={restrict.ToQueryString()}";
        if (maxBookmarkId != null)
            url += $"&max_bookmark_id={maxBookmarkId}";
        if (!string.IsNullOrWhiteSpace(tag))
            url += $"&tag={Uri.EscapeDataString(tag.Trim())}";
        return InvokeGetAsync(
            url,
            PixivJsonContext.Default.IllustList,
            cancellationToken);
    }

    public Task<IllustList> GetIllustFollowAsync(
        Visibility restrict = Visibility.Public,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v2/illust/follow?restrict={restrict.ToQueryString()}&offset={offset}",
            PixivJsonContext.Default.IllustList,
            cancellationToken);
    }

    public Task<IllustComments> GetIllustCommentsAsync(
        int illustId,
        int offset = 0,
        bool includeTotalComments = false,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/illust/comments?illust_id={illustId}&offset={offset}&include_total_comments={(includeTotalComments ? "true" : "false")}",
            PixivJsonContext.Default.IllustComments,
            cancellationToken);
    }

    public Task<PostIllustCommentResult> PostIllustCommentAsync(
        int illustId,
        string comment,
        int? parentCommentId = null,
        CancellationToken cancellationToken = default)
    {
        return InvokePostAsync(
            "v1/illust/comment/add",
            [
                new("illust_id", illustId.ToString(NumberFormatInfo.InvariantInfo)),
                new("comment", comment),
                .. (parentCommentId is int p
                    ? [new("parent_comment_id", p.ToString(NumberFormatInfo.InvariantInfo))]
                    : Array.Empty<KeyValuePair<string, string>>())
            ],
            PixivJsonContext.Default.PostIllustCommentResult,
            cancellationToken);
    }

    public Task<IllustList> GetIllustRelatedAsync(
        int illustId,
        IEnumerable<int>? seedIllustIds = null,
        CancellationToken cancellationToken = default)
    {
        string url = $"v2/illust/related?illust_id={illustId}";
        if (seedIllustIds != null)
            foreach (int seed in seedIllustIds)
                url += $"&seed_illust_ids[]={seed}";
        return InvokeGetAsync(
            url,
            PixivJsonContext.Default.IllustList,
            cancellationToken);
    }

    public Task<RecommendedIllusts> GetRecommendedIllustsAsync(
        UserIllustType contentType = UserIllustType.Illustrations,
        bool includeRankingLabel = true,
        int? maxBookmarkIdForRecommended = null,
        int? minBookmarkIdForRecentIllust = null,
        int offset = 0,
        bool includeRankingIllusts = false,
        IEnumerable<int>? bookmarkIllustIds = null,
        bool includePrivacyPolicy = false,
        CancellationToken cancellationToken = default)
    {
        string url = "v1/illust/recommended";
        url += $"?content_type={contentType.ToQueryString()}"
            + $"&offset={offset}"
            + $"&include_ranking_label={(includeRankingLabel ? "true" : "false")}"
            + $"&include_ranking_illusts={(includeRankingIllusts ? "true" : "false")}"
            + $"&include_privacy_policy={(includePrivacyPolicy ? "true" : "false")}";
        if (maxBookmarkIdForRecommended is int rId)
            url += $"&max_bookmark_id_for_recommend={rId}";
        if (minBookmarkIdForRecentIllust is int iId)
            url += $"&min_bookmark_id_for_recent_illust={iId}";
        if (bookmarkIllustIds != null)
#if NET
            url += $"&bookmark_illust_ids={string.Join(',', bookmarkIllustIds)}";
#else
            url += $"&bookmark_illust_ids={string.Join(",", bookmarkIllustIds)}";
#endif
        return InvokeGetAsync(
            url,
            PixivJsonContext.Default.RecommendedIllusts,
            cancellationToken);
    }

    public async Task<RecommendedIllusts> GetRecommendedIllustsAsyncNoLogin(
        UserIllustType contentType = UserIllustType.Illustrations,
        bool includeRankingLabel = true,
        int? maxBookmarkIdForRecommended = null,
        int? minBookmarkIdForRecentIllust = null,
        int offset = 0,
        bool includeRankingIllusts = false,
        IEnumerable<int>? bookmarkIllustIds = null,
        bool includePrivacyPolicy = false,
        CancellationToken cancellationToken = default)
    {
        string url = "v1/illust/recommended-nologin";
        url += $"?content_type={contentType.ToQueryString()}"
            + $"&offset={offset}"
            + $"&include_ranking_label={(includeRankingLabel ? "true" : "false")}"
            + $"&include_ranking_illusts={(includeRankingIllusts ? "true" : "false")}"
            + $"&include_privacy_policy={(includePrivacyPolicy ? "true" : "false")}";
        if (maxBookmarkIdForRecommended is int rId)
            url += $"&max_bookmark_id_for_recommend={rId}";
        if (minBookmarkIdForRecentIllust is int iId)
            url += $"&min_bookmark_id_for_recent_illust={iId}";
        if (bookmarkIllustIds != null)
#if NET
            url += $"&bookmark_illust_ids={string.Join(',', bookmarkIllustIds)}";
#else
            url += $"&bookmark_illust_ids={string.Join(",", bookmarkIllustIds)}";
#endif
        return (await new HttpClient(InnerHandler, false).GetFromJsonAsync(
            new Uri(url, UriKind.RelativeOrAbsolute),
            PixivJsonContext.Default.RecommendedIllusts,
            cancellationToken)
            .ConfigureAwait(false))
            ?? throw new InvalidOperationException("The api returns top-level null.");
    }

    public Task<IllustList> GetIllustRankingAsync(
        IllustRankingMode mode = IllustRankingMode.Day,
        DateOnly? date = null,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        string url = $"v1/illust/ranking?mode={mode.ToQueryString()}&offset={offset}";
        if (date is not null)
            url += $"&date={date:yyyy-MM-dd}";

        return InvokeGetAsync(
            url,
            PixivJsonContext.Default.IllustList,
            cancellationToken);
    }

    public Task<TrendingTagsIllust> GetTrendingTagsIllustAsync(
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            "v1/trending-tags/illust",
            PixivJsonContext.Default.TrendingTagsIllust,
            cancellationToken);
    }

    public Task<SearchIllustResult> SearchIllustsAsync(
        string word,
        IllustSearchTarget searchTarget = IllustSearchTarget.ExactTag,
        IllustSortMode sort = IllustSortMode.Latest,
        int? maxBookmarkCount = null,
        int? minBookmarkCount = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        string url = $"v1/search/illust?word={HttpUtility.UrlEncode(word)}&search_target={searchTarget.ToQueryString()}"
            + $"&sort={sort.ToQueryString()}&offset={offset}";
        if (maxBookmarkCount is int max)
            url += $"&bookmark_num_max={max}";
        if (minBookmarkCount is int min)
            url += $"&bookmark_num_min={min}";
        if (startDate is not null)
            url += $"&start_date={startDate:yyyy-MM-dd}";
        if (endDate is not null)
            url += $"&end_date={endDate:yyyy-MM-dd}";

        return InvokeGetAsync(
            url,
            PixivJsonContext.Default.SearchIllustResult,
            cancellationToken);
    }

    public Task<UsersList> SearchUsersAsync(
        string word,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/search/user?word={Uri.EscapeDataString(word)}",
            PixivJsonContext.Default.UsersList,
            cancellationToken);
    }

    public Task AddIllustBookmarkAsync(
        int illustId,
        Visibility restrict = Visibility.Public,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string>
        {
            ["illust_id"] = illustId.ToString(NumberFormatInfo.InvariantInfo),
            ["restrict"] = restrict.ToQueryString()
        };
        if (tags != null)
#if NETCOREAPP
            data.Add("tags", string.Join(' ', tags));
#else
            data.Add("tags", string.Join(" ", tags));
#endif

        return InvokePostAsync(
            "v2/illust/bookmark/add",
            [
                new("illust_id", illustId.ToString(NumberFormatInfo.InvariantInfo)),
                new("restrict", restrict.ToQueryString()),
                .. (tags != null
#if NET
                    ? [new("tags", string.Join(' ', tags))]
#else
                    ? [new("tags", string.Join(" ", tags))]
#endif
                    : Array.Empty<KeyValuePair<string, string>>())
            ],
            cancellationToken);
    }

    public Task DeleteIllustBookmarkAsync(
        int illustId,
        CancellationToken cancellationToken = default)
    {
        return InvokePostAsync(
            "v1/illust/bookmark/delete",
            [new("illust_id", illustId.ToString(NumberFormatInfo.InvariantInfo))],
            cancellationToken);
    }

    public Task<UserBookmarkTags> GetUserBookmarkTagsIllustAsync(
        int userId,
        Visibility restrict = Visibility.Public,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/user/bookmark-tags/illust?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
            PixivJsonContext.Default.UserBookmarkTags,
            cancellationToken);
    }

    public Task<UsersList> GetUserFollowingsAsync(
        int userId,
        Visibility restrict = Visibility.Public,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/user/following?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
            PixivJsonContext.Default.UsersList,
            cancellationToken);
    }

    public Task<UsersList> GetUserFollowersAsync(
        int userId,
        Visibility restrict = Visibility.Public,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/user/follower?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
            PixivJsonContext.Default.UsersList,
            cancellationToken);
    }

    public Task AddUserFollowAsync(
        int userId,
        Visibility restrict = Visibility.Public,
        CancellationToken cancellationToken = default)
    {
        return InvokePostAsync(
            "v1/user/follow/add",
            [
                new("user_id", userId.ToString(NumberFormatInfo.InvariantInfo)),
                new("restrict", restrict.ToQueryString()),
            ],
            cancellationToken);
    }

    public Task DeleteUserFollowAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokePostAsync(
            "v1/user/follow/delete",
            [
                new("user_id", userId.ToString(NumberFormatInfo.InvariantInfo)),
            ],
            cancellationToken);
    }

    public Task<UsersList> GetMyPixivUsersAsync(
        int userId,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/user/mypixiv?user_id={userId}&offset={offset}",
            PixivJsonContext.Default.UsersList,
            cancellationToken);
    }

    public Task<AnimatedPictureMetadata> GetAnimatedPictureMetadataAsync(
        int illustId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/ugoira/metadata?illust_id={illustId}",
            PixivJsonContext.Default.AnimatedPictureMetadata,
            cancellationToken);
    }

    public Task<IllustSeriesInfo> GetIllustSeriesAsync(
        int illustSeriesId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/illust/series?illust_series_id={illustSeriesId}",
            PixivJsonContext.Default.IllustSeriesInfo,
            cancellationToken);
    }

    public Task<UserIllustSeries> GetUserIllustSeriesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/user/illust-series?user_id={userId}",
            PixivJsonContext.Default.UserIllustSeries,
            cancellationToken);
    }

    public Task<NovelList> GetUserNovelsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v1/user/novels?user_id={userId}",
            PixivJsonContext.Default.NovelList,
            cancellationToken);
    }

    public Task<NovelDetailResponse> GetNovelDetailAsync(
        int novelId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v2/novel/detail?novel_id={novelId}",
            PixivJsonContext.Default.NovelDetailResponse,
            cancellationToken);
    }

    public Task<NovelSeriesResponse> GetNovelSeriesAsync(
        int seriesId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"v2/novel/series?series_id={seriesId}",
            PixivJsonContext.Default.NovelSeriesResponse,
            cancellationToken);
    }

    public Task<string> GetNovelHtmlAsync(
        int novelId,
        CancellationToken cancellationToken = default)
    {
        return HttpClient.GetStringAsync($"webview/v2/novel?id={novelId}", cancellationToken);
    }

    public Task<HttpResponseMessage> GetImageAsync(Uri imageUri, CancellationToken cancellation = default)
    {
        return HttpClient.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancellation);
    }

    public Task<Stream> GetImageStreamAsync(Uri imageUri, CancellationToken cancellation = default)
    {
        return HttpClient.GetStreamAsync(imageUri, cancellation);
    }

    public async Task<T?> GetNextPageAsync<T>(
        T previous,
        CancellationToken cancellationToken = default)
        where T : class, IHasNextPage
    {
        if (previous.NextUrl is null)
            return null;

        var result = await HttpClient.GetFromJsonAsync(
            previous.NextUrl,
            (JsonTypeInfo<T>)PixivJsonContext.Default.GetTypeInfo(typeof(T))!,
            cancellationToken)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("The api returns top-level null.");
    }

    public async IAsyncEnumerable<TPage> EnumeratePagesAsync<TPage>(
        Task<TPage> initialPage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TPage : class, IHasNextPage
    {
        var page = await initialPage.ConfigureAwait(false);

        while (page is not null)
        {
            yield return page;

            page = await GetNextPageAsync(page, cancellationToken).ConfigureAwait(false);
        }
    }
}
