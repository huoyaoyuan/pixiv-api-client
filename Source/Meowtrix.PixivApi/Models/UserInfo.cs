using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class UserInfo
    {
#pragma warning disable CA1051 // 不要声明可见实例字段
        protected readonly PixivClient Client;
#pragma warning restore CA1051 // 不要声明可见实例字段
        private readonly Uri _avatarUri;

        internal UserInfo(PixivClient client, UserSummary api)
        {
            Client = client;
            Id = api.Id;
            Name = api.Name;
            Account = api.Account;
            IsFollowed = api.IsFollowed;
            Comment = api.Comment;
            _avatarUri = api.ProfileImageUrls.Medium;
        }

        public int Id { get; }
        public string Name { get; }
        public string Account { get; }
        public bool IsFollowed { get; }
        public string? Comment { get; }

        public ImageInfo Avatar => new(_avatarUri, Client);

        public virtual ValueTask<UserDetailInfo> GetDetailAsync(CancellationToken cancellation = default)
            => new(Client.GetUserDetailAsync(Id, cancellation));

        public IAsyncEnumerable<Illust> GetIllustsAsync(UserIllustType? illustType = null,
            CancellationToken cancellation = default)
        {
            return Client.Api.EnumeratePagesAsync(
                Client.Api.GetUserIllustsAsync(userId: Id, illustType: illustType, cancellationToken: cancellation),
                cancellation)
                .SelectMany(x => x.Illusts, (_, x) => new Illust(Client, x));
        }

        public IAsyncEnumerable<Illust> GetBookmarksAsync(CancellationToken cancellation = default)
        {
            return Client.Api.EnumeratePagesAsync(
                Client.Api.GetUserBookmarkIllustsAsync(userId: Id, cancellationToken: cancellation),
                cancellation)
                .SelectMany(x => x.Illusts, (_, x) => new Illust(Client, x));
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetFollowingUsersAsync(CancellationToken cancellation = default)
        {
            return Client.Api.EnumeratePagesAsync(
                Client.Api.GetUserFollowingsAsync(userId: Id, restrict: Visibility.Public, cancellationToken: cancellation),
                cancellation)
                .SelectMany(x => x.UserPreviews, (_, x) => new UserInfoWithPreview(Client, x));
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetFollowerUsersAsync(CancellationToken cancellation = default)
        {
            return Client.Api.EnumeratePagesAsync(
                Client.Api.GetUserFollowersAsync(userId: Id, restrict: Visibility.Public, cancellationToken: cancellation),
                cancellation)
                .SelectMany(x => x.UserPreviews, (_, x) => new UserInfoWithPreview(Client, x));
        }

        public IAsyncEnumerable<IllustSeries> GetIllustSeriesAsync(CancellationToken cancellation = default)
        {
            return Client.Api.EnumeratePagesAsync(
                Client.Api.GetUserIllustSeriesAsync(Id, cancellation),
                cancellation)
                .SelectMany(x => x.IllustSeriesDetails, (_, x) => new IllustSeries(Client, x));
        }

        public IAsyncEnumerable<Novel> GetNovelsAsync(CancellationToken cancellation = default)
        {
            return Client.Api.EnumeratePagesAsync(
                Client.Api.GetUserNovelsAsync(Id, cancellation),
                cancellation)
                .SelectMany(x => x.Novels, (_, x) => new Novel(Client, x));
        }
    }
}
