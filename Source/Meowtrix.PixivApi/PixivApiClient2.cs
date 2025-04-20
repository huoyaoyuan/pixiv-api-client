using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Meowtrix.PixivApi.Authentication;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi;

public class PixivApiClient2 : HttpClient
{
    private const string BaseUrl = "https://app-api.pixiv.net/";
    private static readonly Uri s_baseUri = new(BaseUrl);

    public PixivApiClient2(AccessTokenManager accessTokenManager, HttpMessageHandler? httpMessageHandler = null, bool disposeHandler = true)
        : base(new PixivAccessTokenHandler(accessTokenManager, httpMessageHandler ?? new HttpClientHandler()), disposeHandler)
    {
        BaseAddress = s_baseUri;
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

    private async Task<T> InvokePostAsync<T>(string relativeUri, IEnumerable<KeyValuePair<string, string>> formBody, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(relativeUri, UriKind.RelativeOrAbsolute))
        {
            Content = new FormUrlEncodedContent(formBody),
#if NET
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
#endif
        };
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("The api returns top-level null.");
    }

    public Task<UserDetail> GetUserDetailAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/user/detail?user_id={userId}",
            PixivJsonContext.Default.UserDetail,
            cancellationToken)!;
    }

    public Task<IllustDetailResponse> GetIllustDetailAsync(
        int illustId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/illust/detail?illust_id={illustId}",
            PixivJsonContext.Default.IllustDetailResponse,
            cancellationToken);
    }

    public Task<IllustList> GetUserIllustsAsync(
        int userId,
        UserIllustType? illustType = null,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        string url = $"/v1/user/illusts?user_id={userId}&offset={offset}";
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
        string url = $"/v1/user/bookmarks/illust?user_id={userId}&restrict={restrict.ToQueryString()}";
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
            $"/v2/illust/follow?restrict={restrict.ToQueryString()}&offset={offset}",
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
            $"/v1/illust/comments?illust_id={illustId}&offset={offset}&include_total_comments={(includeTotalComments ? "true" : "false")}",
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
            "/v1/illust/comment/add",
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
        string url = $"/v2/illust/related?illust_id={illustId}";
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
        string url = "/v1/illust/recommended";
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

    public Task<IllustList> GetIllustRankingAsync(
        IllustRankingMode mode = IllustRankingMode.Day,
        DateOnly? date = null,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        string url = $"/v1/illust/ranking?mode={mode.ToQueryString()}&offset={offset}";
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
            "/v1/trending-tags/illust",
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
        string url = $"/v1/search/illust?word={HttpUtility.UrlEncode(word)}&search_target={searchTarget.ToQueryString()}"
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
            $"/v1/search/user?word={Uri.EscapeDataString(word)}",
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
            "/v2/illust/bookmark/add",
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
            PixivJsonContext.Default.Object,
            cancellationToken);
    }

    public Task DeleteIllustBookmarkAsync(
        int illustId,
        CancellationToken cancellationToken = default)
    {
        return InvokePostAsync(
            "/v1/illust/bookmark/delete",
            [new("illust_id", illustId.ToString(NumberFormatInfo.InvariantInfo))],
            PixivJsonContext.Default.Object,
            cancellationToken);
    }

    public Task<UserBookmarkTags> GetUserBookmarkTagsIllustAsync(
        Visibility restrict = Visibility.Public,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/user/bookmark-tags/illust?restrict={restrict.ToQueryString()}&offset={offset}",
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
            $"/v1/user/following?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
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
            $"/v1/user/follower?user_id={userId}&restrict={restrict.ToQueryString()}&offset={offset}",
            PixivJsonContext.Default.UsersList,
            cancellationToken);
    }

    public Task AddUserFollowAsync(
        int userId,
        Visibility restrict = Visibility.Public,
        CancellationToken cancellationToken = default)
    {
        return InvokePostAsync(
            "/v1/user/follow/add",
            [
                new("user_id", userId.ToString(NumberFormatInfo.InvariantInfo)),
                new("restrict", restrict.ToQueryString()),
            ],
            PixivJsonContext.Default.Object,
            cancellationToken);
    }

    public Task DeleteUserFollowAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokePostAsync(
            "/v1/user/follow/delete",
            [
                new("user_id", userId.ToString(NumberFormatInfo.InvariantInfo)),
            ],
            PixivJsonContext.Default.Object,
            cancellationToken);
    }

    public Task<UsersList> GetMyPixivUsersAsync(
        int userId,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/user/mypixiv?user_id={userId}&offset={offset}",
            PixivJsonContext.Default.UsersList,
            cancellationToken);
    }

    public Task<AnimatedPictureMetadata> GetAnimatedPictureMetadataAsync(
        int illustId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/ugoira/metadata?illust_id={illustId}",
            PixivJsonContext.Default.AnimatedPictureMetadata,
            cancellationToken);
    }

    public Task<IllustSeriesInfo> GetIllustSeriesAsync(
        int illustSeriesId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/illust/series?illust_series_id={illustSeriesId}",
            PixivJsonContext.Default.IllustSeriesInfo,
            cancellationToken);
    }

    public Task<UserIllustSeries> GetUserIllustSeriesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/user/illust-series?user_id={userId}",
            PixivJsonContext.Default.UserIllustSeries,
            cancellationToken);
    }

    public Task<UserNovels> GetUserNovelsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/user/novels?user_id={userId}",
            PixivJsonContext.Default.UserNovels,
            cancellationToken);
    }

    public Task<NovelDetailResponse> GetNovelDetailAsync(
        int novelId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v2/novel/detail?novel_id={novelId}",
            PixivJsonContext.Default.NovelDetailResponse,
            cancellationToken);
    }

    public Task<NovelTextResponse> GetNovelTextAsync(
        int novelId,
        CancellationToken cancellationToken = default)
    {
        return InvokeGetAsync(
            $"/v1/novel/text?novel_id={novelId}",
            PixivJsonContext.Default.NovelTextResponse,
            cancellationToken);
    }

    public async Task<HttpResponseMessage> GetImageAsync(Uri imageUri,
        CancellationToken cancellation = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, imageUri)
        {
            Headers =
            {
                Referrer = s_baseUri
            },
#if NET5_0_OR_GREATER
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
#endif
        };

        return (await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
            .EnsureSuccessStatusCode();
    }

    public async ValueTask<T?> GetNextPageAsync<T>(
        T previous,
        CancellationToken cancellationToken = default)
        where T : class, IHasNextPage
    {
        if (previous.NextUrl is null)
            return null;

        var result = await this.GetFromJsonAsync(
            previous.NextUrl,
            (JsonTypeInfo<T>)PixivJsonContext.Default.GetTypeInfo(typeof(T))!,
            cancellationToken)
            .ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("The api returns top-level null.");
    }
}
