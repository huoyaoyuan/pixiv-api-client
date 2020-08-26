using System;
using System.Collections.Immutable;
using System.Text.Json;

#nullable disable
#pragma warning disable IDE1006 // Naming style

namespace Meowtrix.PixivApi.Json
{
    public record AuthResult(Response Response);

    public record Response(
        string AccessToken,
        int ExpiresIn,
        string TokenType,
        string Scope,
        string RefreshToken,
        AuthUser User,
        string DeviceToken);

    public record AuthUser(
        ProfileImageUrls ProfileImageUrls,
        string Id,
        string Name,
        string Account,
        string MainAddress,
        bool IsPremium,
        int XRestrict,
        bool IsMainAuthorized);

    public record ProfileImageUrls(Uri Px16x16, Uri Px50x50, Uri Px170x170);

    public record UserDetail(
        IllustUser User,
        UserProfile Profile,
        ProfilePublicity ProfilePublicity,
        Workspace Workspace);

    public record IllustUser(
        int Id,
        string Name,
        string Account,
        ProfileImageUrls ProfileImageUrls,
        bool IsFollowed,
        string Comment);

    public record UserProfile(
        JsonElement WebPage,
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
        Uri BackgroundImageUrl,
        string TwitterAccount,
        Uri TwitterUrl,
        Uri PawooUrl,
        bool IsPremium,
        bool IsUsingCustomProfileImage);

    public record ProfilePublicity(
        string Gender,
        string Region,
        string BirthDay,
        string BirthYear,
        string Job,
        bool Pawoo);

    public record Workspace(
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
        Uri WorkspaceImageUrl);

    public record UserIllusts(ImmutableArray<UserIllustPreview> Illusts, Uri NextUrl);

    public record UserIllustPreview(
        int Id,
        string Title,
        string Type,
        SizedImageUrls ImageUrls,
        string Caption,
        int Restrict,
        IllustUser User,
        ImmutableArray<IllustTag> Tags,
        ImmutableArray<string> Tools,
        DateTimeOffset CreateDate,
        int PageCount,
        int Width,
        int Height,
        int SanityLevel,
        int XRestrict,
        UserIllustPreview.IllustSeries Series,
        UserIllustPreview.MetaSingle MetaSinglePage,
        ImmutableArray<UserIllustPreview.MetaPage> MetaPages,
        int TotalView,
        int TotalBookmarks,
        bool IsBookmarked,
        bool Visible,
        bool IsMuted,
        int TotalComments)
    {
        public record IllustSeries(int Id, string Title);

        public record MetaSingle(Uri OriginalImageUrl);

        public record MetaPage(SizedImageUrls ImageUrls);
    }

    public record SizedImageUrls(
        Uri SquareMedium,
        Uri Medium,
        Uri Large,
        Uri Original);

    public record IllustTag(string Name, string TranslatedName);

    public record IllustComments(int TotalComments, ImmutableArray<IllustComment> Comments, Uri NextUri);

    public record IllustComment(
        int Id,
        string Comment,
        DateTimeOffset Date,
        IllustUser User,
        bool HasReplies,
        IllustComment ParentComment);

    public record PostIllustCommentResult(IllustComment Comment);

    public record RecommendedIllusts(
        ImmutableArray<UserIllustPreview> Illusts,
        ImmutableArray<object> RankingIllusts,
        bool ContestExists,
        JsonElement PrivacyPolicy,
        Uri NextUrl);

    public record TrendingTagsIllust(ImmutableArray<TrendTag> TrendTags);

    public record TrendTag(string Tag, string TranslatedName, UserIllustPreview Illust);

    public record SearchIllustResult(ImmutableArray<UserIllustPreview> Illusts, Uri NextUrl, int SearchSpanLimit);

    public record UserBookmarkTags(ImmutableArray<JsonElement> BookmarkTags, Uri NextUrl);

    public record UserFollowList(ImmutableArray<UserPreview> UserPreviews, Uri NextUrl);

    public record UserPreview(
        IllustUser User,
        ImmutableArray<UserIllustPreview> Illusts,
        ImmutableArray<JsonElement> Novels,
        bool IsMuted);

    public record MotionPicMetadata(MotionPicMetadata.MetadataClass UgoiraMetadata)
    {
        public record MetadataClass(Urls ZipUrls, ImmutableArray<Frame> Frames);

        public record Urls(Uri Medium);

        public record Frame(string File, int Delay);
    }
}
