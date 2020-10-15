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

#pragma warning disable CA1801, IDE0060 // false positive
    public sealed record AuthProfileImageUrls(
        [property: JsonPropertyName("px_16x16")]
        Uri PixelSize16,
        [property: JsonPropertyName("px_50x50")]
        Uri PixelSize50,
        [property: JsonPropertyName("px_170x170")]
        Uri PixelSize170);
#pragma warning restore CA1801

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
        : IHasNextPage;

    public sealed record IllustDetailResponse(IllustDetail Illust);

    public sealed record IllustDetail(
        int Id,
        string Title,
        string Type,
        IllustDetail.PreviewImageUrls ImageUrls,
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
        IllustDetail.IllustSeries Series,
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

        public sealed record MetaPage(MetaPageImageUrls ImageUrls);

        public record PreviewImageUrls(
            Uri SquareMedium,
            Uri Medium,
            Uri Large);

        public record MetaPageImageUrls(
            Uri SquareMedium,
            Uri Medium,
            Uri Large,
            Uri Original);
    }

    public sealed record IllustTag(string Name, string? TranslatedName);

    public sealed record IllustComments(int TotalComments, ImmutableArray<IllustComment> Comments, Uri? NextUrl)
        : IHasNextPage;

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
        : IHasNextPage;

    public sealed record UserBookmarkTags(ImmutableArray<object> BookmarkTags, Uri? NextUrl)
        : IHasNextPage;

    public sealed record UserFollowList(ImmutableArray<UserPreview> UserPreviews, Uri? NextUrl)
        : IHasNextPage;

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
}
