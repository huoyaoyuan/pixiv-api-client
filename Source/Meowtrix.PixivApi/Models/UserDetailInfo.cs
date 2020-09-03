using System;
using System.Net.Http;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class UserDetailInfo : UserInfo
    {
        private readonly UserProfile _profile;
        private readonly Workspace _workspace;

        public UserDetailInfo(PixivClient client, UserDetail api)
            : base(client, api.User)
        {
            _profile = api.Profile;
            _workspace = api.Workspace;
        }

        public override ValueTask<UserDetailInfo> GetDetailAsync() => new(this);

        public Uri? WebPage => _profile.WebPage;
        public Gender Gender => _profile.Gender switch
        {
            "male" => Gender.Male,
            "female" => Gender.Female,
            _ => Gender.Unknown
        };
        public string Region => _profile.Region;

        public int TotalFollowUsers => _profile.TotalFollowUsers;
        public int TotalMyPixivUsers => _profile.TotalMyPixivUsers;
        public int TotalManga => _profile.TotalManga;
        public int TotalNovels => _profile.TotalNovels;
        public int TotalIllustBookmarksPublic => _profile.TotalIllustBookmarksPublic;
        public int TotalIllustSeries => _profile.TotalIllustSeries;
        public int TotalNovelSeries => _profile.TotalNovelSeries;

        public Task<HttpResponseMessage?> GetBackgroundImageAsync()
            => (_profile.BackgroundImageUrl is null
            ? Task.FromResult<HttpResponseMessage?>(null)!
            : Client.Api.GetImageAsync(_profile.BackgroundImageUrl))!;

        public string? TwitterAccount => _profile.TwitterAccount;

        public Task<HttpResponseMessage?> GetWorkspaceImageAsync()
            => (_workspace.WorkspaceImageUrl is null
            ? Task.FromResult<HttpResponseMessage?>(null)!
            : Client.Api.GetImageAsync(_workspace.WorkspaceImageUrl))!;
    }

    public enum Gender
    {
        Unknown,
        Male,
        Female
    }
}
