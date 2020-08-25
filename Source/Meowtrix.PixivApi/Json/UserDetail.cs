using System;

#nullable disable

namespace Meowtrix.PixivApi.Json
{
    public class UserDetail
    {
        public IllustUser User { get; set; }
        public UserProfile Profile { get; set; }
        public ProfilePublicity ProfilePublicity { get; set; }
        public Workspace Workspace { get; set; }
    }

    public class IllustUser
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Account { get; set; }
        public ProfileImageUrls ProfileImageUrls { get; set; }
        public bool? IsFollowed { get; set; }
        public string Comment { get; set; }
    }

    public class UserProfile
    {
        public object WebPage { get; set; }
        public string Gender { get; set; }
        public string Birth { get; set; }
        public string BirthDay { get; set; }
        public int BirthYear { get; set; }
        public string Region { get; set; }
        public int AddressId { get; set; }
        public string CountryCode { get; set; }
        public string Job { get; set; }
        public int JobId { get; set; }
        public int TotalFollowUsers { get; set; }
        public int TotalMyPixivUsers { get; set; }
        public int TotalIllusts { get; set; }
        public int TotalManga { get; set; }
        public int TotalNovels { get; set; }
        public int TotalIllustBookmarksPublic { get; set; }
        public int TotalIllustSeries { get; set; }
        public int TotalNovelSeries { get; set; }
        public Uri BackgroundImageUrl { get; set; }
        public string TwitterAccount { get; set; }
        public Uri TwitterUrl { get; set; }
        public Uri PawooUrl { get; set; }
        public bool IsPremium { get; set; }
        public bool IsUsingCustomProfileImage { get; set; }
    }

    public class ProfilePublicity
    {
        public string Gender { get; set; }
        public string Region { get; set; }
        public string BirthDay { get; set; }
        public string BirthYear { get; set; }
        public string Job { get; set; }
        public bool Pawoo { get; set; }
    }

    public class Workspace
    {
        public string Pc { get; set; }
        public string Monitor { get; set; }
        public string Tool { get; set; }
        public string Scanner { get; set; }
        public string Tablet { get; set; }
        public string Mouse { get; set; }
        public string Printer { get; set; }
        public string Desktop { get; set; }
        public string Music { get; set; }
        public string Desk { get; set; }
        public string Chair { get; set; }
        public string Comment { get; set; }
        public Uri WorkspaceImageUrl { get; set; }
    }
}
