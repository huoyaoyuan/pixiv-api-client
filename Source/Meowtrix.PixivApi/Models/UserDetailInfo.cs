using System;
using System.Threading;
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

        public override ValueTask<UserDetailInfo> GetDetailAsync(CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            return new(this);
        }

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
        public int TotalIllusts => _profile.TotalIllusts;
        public int TotalManga => _profile.TotalManga;
        public int TotalNovels => _profile.TotalNovels;
        public int TotalIllustBookmarksPublic => _profile.TotalIllustBookmarksPublic;
        public int TotalIllustSeries => _profile.TotalIllustSeries;
        public int TotalNovelSeries => _profile.TotalNovelSeries;

        public ImageInfo? BackgroundImage
            => _profile.BackgroundImageUrl is null
            ? null
            : new ImageInfo(_profile.BackgroundImageUrl, Client.Api);

        public string? TwitterAccount => _profile.TwitterAccount;

        public ImageInfo? WorkspaceImage
            => _workspace.WorkspaceImageUrl is null
            ? null
            : new ImageInfo(_workspace.WorkspaceImageUrl, Client.Api);
    }

    public enum Gender
    {
        Unknown,
        Male,
        Female
    }
}
