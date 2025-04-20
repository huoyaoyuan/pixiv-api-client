using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meowtrix.PixivApi.Json
{
    public sealed record AuthResponse(
        string AccessToken,
        int ExpiresIn,
        string TokenType,
        string Scope,
        string RefreshToken,
        AuthUser User);

    public sealed record AuthUser(
        AuthProfileImageUrls ProfileImageUrls,
        string Id,
        string Name,
        string Account,
        string MailAddress,
        bool IsPremium,
        int XRestrict,
        bool IsMailAuthorized);

    public sealed record AuthProfileImageUrls(
        [property: JsonPropertyName("px_16x16")]
        Uri PixelSize16,
        [property: JsonPropertyName("px_50x50")]
        Uri PixelSize50,
        [property: JsonPropertyName("px_170x170")]
        Uri PixelSize170);

    public sealed record UserDetail(
        UserSummary User,
        UserProfile Profile,
        ProfilePublicity ProfilePublicity,
        Workspace Workspace);

    public sealed record UserSummary(
        int Id,
        string Name,
        string Account,
        UserSummary.ImageUrls ProfileImageUrls,
        bool IsFollowed,
        string? Comment)
    {
        public sealed record ImageUrls(Uri Medium);
    }

    public sealed record UserProfile(
        Uri? WebPage,
        string Gender,
        string Birth,
        string BirthDay,
        int BirthYear,
        string Region,
        int AddressId,
        string CountryCode,
        string Job,
        int JobId,
        int TotalFollowUsers,
        int TotalMyPixivUsers,
        int TotalIllusts,
        int TotalManga,
        int TotalNovels,
        int TotalIllustBookmarksPublic,
        int TotalIllustSeries,
        int TotalNovelSeries,
        Uri? BackgroundImageUrl,
        string? TwitterAccount,
        Uri? TwitterUrl,
        Uri? PawooUrl,
        bool IsPremium,
        bool IsUsingCustomProfileImage);

    public sealed record ProfilePublicity(
        string Gender,
        string Region,
        string BirthDay,
        string BirthYear,
        string Job,
        bool Pawoo);

    public sealed record Workspace(
        string Pc,
        string Monitor,
        string Tool,
        string Scanner,
        string Tablet,
        string Mouse,
        string Printer,
        string Desktop,
        string Music,
        string Desk,
        string Chair,
        string Comment,
        Uri? WorkspaceImageUrl);

    public sealed record UserIllusts(ImmutableArray<IllustDetail> Illusts, Uri? NextUrl)
        : IHasNextPage<IllustDetail>
    {
        ImmutableArray<IllustDetail> IHasNextPage<IllustDetail>.Items => Illusts;
    }

    public sealed record IllustDetailResponse(IllustDetail Illust);

    public sealed record IllustDetail(
        int Id,
        string Title,
        string Type,
        PreviewImageUrls ImageUrls,
        string Caption,
        int Restrict,
        UserSummary User,
        ImmutableArray<IllustTag> Tags,
        ImmutableArray<string> Tools,
        DateTimeOffset CreateDate,
        int PageCount,
        int Width,
        int Height,
        int SanityLevel,
        int XRestrict,
        IllustDetail.IllustSeries? Series,
        IllustDetail.MetaSingle MetaSinglePage,
        ImmutableArray<IllustDetail.MetaPage> MetaPages,
        int TotalView,
        int TotalBookmarks,
        bool IsBookmarked,
        bool Visible,
        bool IsMuted,
        int TotalComments)
    {
        public sealed record IllustSeries(int Id, string Title);

        public sealed record MetaSingle(Uri? OriginalImageUrl);

        public sealed record MetaPage(OriginalImageUrls ImageUrls);
    }

    public record PreviewImageUrls(
        Uri SquareMedium,
        Uri Medium,
        Uri Large);

    public record OriginalImageUrls(
        Uri SquareMedium,
        Uri Medium,
        Uri Large,
        Uri Original);

    public sealed record IllustTag(string Name, string? TranslatedName);

    public sealed record IllustComments(int TotalComments, ImmutableArray<IllustComment> Comments, Uri? NextUrl)
        : IHasNextPage<IllustComment>
    {
        ImmutableArray<IllustComment> IHasNextPage<IllustComment>.Items => Comments;
    }

    public sealed record IllustComment(
        int Id,
        string Comment,
        DateTimeOffset Date,
        UserSummary User,
        bool HasReplies,
        IllustComment ParentComment);

    public sealed record PostIllustCommentResult(IllustComment Comment);

    public sealed record RecommendedIllusts(
        ImmutableArray<IllustDetail> Illusts,
        ImmutableArray<object> RankingIllusts,
        bool ContestExists,
        JsonElement PrivacyPolicy,
        Uri? NextUrl)
        : IHasNextPage;

    public sealed record TrendingTagsIllust(ImmutableArray<TrendTag> TrendTags);

    public sealed record TrendTag(string Tag, string? TranslatedName, IllustDetail Illust);

    public sealed record SearchIllustResult(ImmutableArray<IllustDetail> Illusts, Uri? NextUrl, int SearchSpanLimit)
        : IHasNextPage<IllustDetail>
    {
        ImmutableArray<IllustDetail> IHasNextPage<IllustDetail>.Items => Illusts;
    }

    public sealed record UserBookmarkTags(ImmutableArray<object> BookmarkTags, Uri? NextUrl)
        : IHasNextPage;

    public sealed record UsersList(ImmutableArray<UserPreview> UserPreviews, Uri? NextUrl)
        : IHasNextPage<UserPreview>
    {
        ImmutableArray<UserPreview> IHasNextPage<UserPreview>.Items => UserPreviews;
    }

    public sealed record UserPreview(
        UserSummary User,
        ImmutableArray<IllustDetail> Illusts,
        ImmutableArray<object> Novels,
        bool IsMuted);

    public sealed record AnimatedPictureMetadata(AnimatedPictureMetadata.MetadataClass UgoiraMetadata)
    {
        public sealed record MetadataClass(Urls ZipUrls, ImmutableArray<Frame> Frames);

        public sealed record Urls(Uri Medium);

        public sealed record Frame(string File, int Delay);
    }

    public sealed record IllustSeriesDetails(
        int Id,
        string Title,
        string Caption,
        UserSummary.ImageUrls CoverImageUrls,
        int SeriesWorkCount,
        DateTimeOffset CreateDate,
        int Width,
        int Height,
        UserSummary User);

    public sealed record IllustSeriesInfo(
        IllustSeriesDetails IllustSeriesDetail,
        IllustDetail IllustSeriesFirstIllust,
        ImmutableArray<IllustDetail> Illusts,
        Uri? NextUrl)
        : IHasNextPage<IllustDetail>
    {
        ImmutableArray<IllustDetail> IHasNextPage<IllustDetail>.Items => Illusts;
    }

    public sealed record UserIllustSeries(
        ImmutableArray<IllustSeriesDetails> IllustSeriesDetails,
        Uri? NextUrl)
        : IHasNextPage<IllustSeriesDetails>
    {
        ImmutableArray<IllustSeriesDetails> IHasNextPage<IllustSeriesDetails>.Items => IllustSeriesDetails;
    }

    public sealed record UserNovels(ImmutableArray<NovelDetail> Novels, Uri? NextUrl)
        : IHasNextPage<NovelDetail>
    {
        ImmutableArray<NovelDetail> IHasNextPage<NovelDetail>.Items => Novels;
    }

    public sealed record NovelDetail(
        int Id,
        string Title,
        string Caption,
        int Restrict,
        int XRestrict,
        bool IsOriginal,
        PreviewImageUrls ImageUrls,
        DateTimeOffset CreateDate,
        ImmutableArray<NovelDetail.NovelTag> Tags,
        int PageCount,
        int TextLength,
        UserSummary User,
        NovelDetail.NovelSeries? Series,
        bool IsBookmarked,
        int TotalBookmarks,
        int TotalView,
        bool Visible,
        int TotalComments,
        bool IsMuted,
        bool IsMyPixivOnly,
        bool IsXRestricted,
        int NovelAiType)
    {
        public sealed record NovelTag(string Name, string? TranslatedName, bool AddedByUploadedUser);
        public sealed record NovelSeries(int Id, string Title);
    }

    public sealed record NovelDetailResponse(NovelDetail Novel);

    public sealed record NovelTextResponse(object NovelMarker, string NovelText);

    [JsonSerializable(typeof(AuthUser))]
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true)]
    internal partial class PixivJsonContext : JsonSerializerContext { }
}
