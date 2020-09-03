using System;
using System.Collections.Generic;
using System.Net.Http;
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
        public bool IsFollowed { get; private set; }
        public string Comment { get; }

        public async Task FollowAsync(Visibility visibility = Visibility.Public)
        {
            if (IsFollowed)
                throw new InvalidOperationException("The user has already been followed!");

            await Client.Api.AddUserFollowAsync(Id, visibility,
                await Client.CheckValidAccessToken().ConfigureAwait(false)).ConfigureAwait(false);

            IsFollowed = true;
        }

        public async Task UnfollowAsync(Visibility visibility = Visibility.Public)
        {
            if (!IsFollowed)
                throw new InvalidOperationException("The user has not been followed!");

            await Client.Api.DeleteUserFollowAsync(Id, visibility,
                await Client.CheckValidAccessToken().ConfigureAwait(false)).ConfigureAwait(false);

            IsFollowed = false;
        }

        public Task<HttpResponseMessage> GetAvatarAsync() => Client.Api.GetImageAsync(_avatarUri);

        public virtual ValueTask<UserDetailInfo> GetDetailAsync() => new(Client.GetUserDetailAsync(Id));

        public IAsyncEnumerable<Illust> GetIllustsAsync(UserIllustType illustType = UserIllustType.Illustrations)
        {
            return Client.ToAsyncEnumerable(auth
                => Client.Api.GetUserIllustsAsync(Id, illustType, authToken: auth));
        }

        public IAsyncEnumerable<Illust> GetBookmarksAsync()
        {
            return Client.ToAsyncEnumerable(auth
                => Client.Api.GetUserBookmarkIllustsAsync(Id));
        }
    }
}
