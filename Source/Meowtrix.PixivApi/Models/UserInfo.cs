using System;
using System.Collections.Generic;
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
            return Client.ToIllustAsyncEnumerable(async (auth, c)
                => await Client.Api.GetUserIllustsAsync(authToken: auth, userId: Id, illustType: illustType, cancellation: c).ConfigureAwait(false),
                cancellation);
        }

        public IAsyncEnumerable<Illust> GetBookmarksAsync(CancellationToken cancellation = default)
        {
            return Client.ToIllustAsyncEnumerable(async (auth, c)
                => await Client.Api.GetUserBookmarkIllustsAsync(authToken: auth, userId: Id, cancellation: c).ConfigureAwait(false),
                cancellation);
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetFollowingUsersAsync(CancellationToken cancellation = default)
        {
            return Client.ToUserAsyncEnumerable(async (auth, c)
                => await Client.Api.GetUserFollowingsAsync(authToken: auth, userId: Id, restrict: Visibility.Public, cancellation: c).ConfigureAwait(false),
                cancellation);
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetFollowerUsersAsync(CancellationToken cancellation = default)
        {
            return Client.ToUserAsyncEnumerable(async (auth, c)
                => await Client.Api.GetUserFollowersAsync(authToken: auth, userId: Id, restrict: Visibility.Public, cancellation: c).ConfigureAwait(false),
                cancellation);
        }
    }
}
