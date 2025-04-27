using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meowtrix.PixivApi.Json;

public sealed class AuthUser
{
    public required AuthProfileImageUrls ProfileImageUrls { get; init; }

    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Account { get; init; }

    public string? MailAddress { get; init; }

    public bool IsPremium { get; init; }

    public int XRestrict { get; init; }

    public bool IsMailAuthorized { get; init; }

    public sealed record AuthProfileImageUrls(
        [property: JsonPropertyName("px_16x16")]
        Uri PixelSize16,
        [property: JsonPropertyName("px_50x50")]
        Uri PixelSize50,
        [property: JsonPropertyName("px_170x170")]
        Uri PixelSize170);
}

public sealed class UserDetail
{
    public required UserSummary User { get; init; }

    public required UserProfile Profile { get; init; }

    public required ProfilePublicity ProfilePublicity { get; init; }

    public required Workspace Workspace { get; init; }
}

public sealed class UserSummary
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Account { get; init; }

    public required MediumImageUrls ProfileImageUrls { get; init; }

    public bool IsFollowed { get; init; }

    public string? Comment { get; init; }
}

public sealed record UserProfile
{
    public Uri? WebPage { get; init; }

    public string? Gender { get; init; }

    public string? Birth { get; init; }

    public string? BirthDay { get; init; }

    public int BirthYear { get; init; }

    public string? Region { get; init; }

    public int AddressId { get; init; }

    public string? CountryCode { get; init; }

    public string? Job { get; init; }

    public int JobId { get; init; }

    public int TotalFollowUsers { get; init; }

    public int TotalMypixivUsers { get; init; }

    public int TotalIllusts { get; init; }

    public int TotalManga { get; init; }

    public int TotalNovels { get; init; }

    public int TotalIllustBookmarksPublic { get; init; }

    public int TotalIllustSeries { get; init; }

    public int TotalNovelSeries { get; init; }

    public Uri? BackgroundImageUrl { get; init; }

    public required string? TwitterAccount { get; init; }

    public Uri? TwitterUrl { get; init; }

    public Uri? PawooUrl { get; init; }

    public required bool IsPremium { get; init; }

    public required bool IsUsingCustomProfileImage { get; init; }
}

public sealed class ProfilePublicity
{
    public string? Gender { get; init; }

    public string? Region { get; init; }

    public string? BirthDay { get; init; }

    public string? BirthYear { get; init; }

    public string? Job { get; init; }

    public bool Pawoo { get; init; }
}

public sealed class Workspace
{
    public string? Pc { get; init; }

    public string? Monitor { get; init; }

    public string? Tool { get; init; }

    public string? Scanner { get; init; }

    public string? Tablet { get; init; }

    public string? Mouse { get; init; }

    public string? Printer { get; init; }

    public string? Desktop { get; init; }

    public string? Music { get; init; }

    public string? Desk { get; init; }

    public string? Chair { get; init; }

    public string? Comment { get; init; }

    public Uri? WorkspaceImageUrl { get; init; }
}

public sealed record IllustList(ImmutableArray<IllustDetail> Illusts, Uri? NextUrl)
    : IHasNextPage<IllustDetail>
{
    ImmutableArray<IllustDetail> IHasNextPage<IllustDetail>.Items => Illusts;
}

public sealed record IllustDetailResponse(IllustDetail Illust);

public sealed record IllustDetail
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Type { get; init; }

    public required PreviewImageUrls ImageUrls { get; init; }

    public required string Caption { get; init; }

    public int Restrict { get; init; }

    public required UserSummary User { get; init; }

    public ImmutableArray<IllustTag> Tags { get; init; } = [];

    public ImmutableArray<string> Tools { get; init; } = [];

    public required DateTimeOffset CreateDate { get; init; }

    public required int PageCount { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public int SanityLevel { get; init; }

    public int XRestrict { get; init; }

    public IllustSeries? Series { get; init; }

    public required MetaSingle MetaSinglePage { get; init; }

    public ImmutableArray<MetaPage> MetaPages { get; init; } = [];

    public int TotalView { get; init; }

    public int TotalBookmarks { get; init; }

    public bool IsBookmarked { get; init; }

    public bool Visible { get; init; }

    public bool IsMuted { get; init; }

    public int TotalComments { get; init; }

    public sealed record IllustSeries(int Id, string Title);

    public sealed record MetaSingle(Uri? OriginalImageUrl);

    public sealed record MetaPage(OriginalImageUrls ImageUrls);
}

public sealed record MediumImageUrls(Uri Medium);

public sealed record PreviewImageUrls(
    Uri SquareMedium,
    Uri Medium,
    Uri Large);

public sealed record OriginalImageUrls(
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

public sealed class IllustComment
{
    public required int Id { get; init; }

    public required string Comment { get; init; }

    public required DateTimeOffset Date { get; init; }

    public required UserSummary User { get; init; }

    public bool HasReplies { get; init; }

    public IllustComment? ParentComment { get; init; }
}

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

public sealed class UserPreview
{
    public required UserSummary User { get; init; }

    public ImmutableArray<IllustDetail> Illusts { get; init; } = [];

    public ImmutableArray<NovelDetail> Novels { get; init; } = [];

    public bool IsMuted { get; init; }
}

public sealed record AnimatedPictureMetadata(AnimatedPictureMetadata.MetadataClass UgoiraMetadata)
{
    public sealed record MetadataClass(MediumImageUrls ZipUrls, ImmutableArray<Frame> Frames);

    public sealed record Frame(string File, int Delay);
}

public sealed record IllustSeriesDetails
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Caption { get; init; }

    public required MediumImageUrls CoverImageUrls { get; init; }

    public int SeriesWorkCount { get; init; }

    public required DateTimeOffset CreateDate { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public required UserSummary User { get; init; }
}

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

public sealed record NovelList(ImmutableArray<NovelDetail> Novels, Uri? NextUrl)
    : IHasNextPage<NovelDetail>
{
    ImmutableArray<NovelDetail> IHasNextPage<NovelDetail>.Items => Novels;
}

public sealed record NovelDetail
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Caption { get; init; }

    public int Restrict { get; init; }

    public int XRestrict { get; init; }

    public bool IsOriginal { get; init; }

    public required PreviewImageUrls ImageUrls { get; init; }

    public required DateTimeOffset CreateDate { get; init; }

    public ImmutableArray<NovelTag> Tags { get; init; } = [];

    public int PageCount { get; init; }

    public int TextLength { get; init; }

    public required UserSummary User { get; init; }

    public NovelSeries? Series { get; init; }

    public bool IsBookmarked { get; init; }

    public int TotalBookmarks { get; init; }

    public int TotalView { get; init; }

    public bool Visible { get; init; }

    public int TotalComments { get; init; }

    public bool IsMuted { get; init; }

    public bool IsMypixivOnly { get; init; }

    public bool IsXRestricted { get; init; }

    public int NovelAiType { get; init; }

    public sealed record NovelTag(string Name, string? TranslatedName, bool AddedByUploadedUser);

    public sealed record NovelSeries(int Id, string Title);
}

public sealed record NovelDetailResponse(NovelDetail Novel);

public record class NovelSeriesDetail
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public required string Caption { get; init; }

    public bool IsOriginal { get; init; }

    public bool IsConcluded { get; init; }

    public required int ContentCount { get; init; }

    public int TotalCharacterCount { get; init; }

    public required UserSummary User { get; init; }

    public required string DisplayText { get; init; }

    public int NovelAiType { get; init; }

    public bool WatchlistAdded { get; init; }
}

public record class NovelSeriesResponse(
    NovelSeriesDetail NovelSeriesDetail,
    NovelDetail NovelSeriesFirstNovel,
    NovelDetail NovelSeriesLatestNovel,
    ImmutableArray<NovelDetail> Novels,
    Uri? NextUrl)
    : IHasNextPage<NovelDetail>
{
    public ImmutableArray<NovelDetail> Items => Novels;
}

[JsonSerializable(typeof(AuthUser))]
[JsonSerializable(typeof(UserDetail))]
[JsonSerializable(typeof(IllustDetailResponse))]
[JsonSerializable(typeof(IllustList))]
[JsonSerializable(typeof(IllustComments))]
[JsonSerializable(typeof(PostIllustCommentResult))]
[JsonSerializable(typeof(RecommendedIllusts))]
[JsonSerializable(typeof(TrendingTagsIllust))]
[JsonSerializable(typeof(SearchIllustResult))]
[JsonSerializable(typeof(UsersList))]
[JsonSerializable(typeof(UserBookmarkTags))]
[JsonSerializable(typeof(AnimatedPictureMetadata))]
[JsonSerializable(typeof(IllustSeriesInfo))]
[JsonSerializable(typeof(UserIllustSeries))]
[JsonSerializable(typeof(NovelList))]
[JsonSerializable(typeof(NovelDetailResponse))]
[JsonSerializable(typeof(NovelSeriesResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = false)]
internal partial class PixivJsonContext : JsonSerializerContext;
