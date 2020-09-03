using System;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class User
    {
        private readonly PixivClient _client;

        internal User(PixivClient client, UserDetail api)
        {
            _client = client;
            Id = api.User.Id;
            Name = api.User.Name;
            Account = api.User.Account;
            IsFollowed = api.User.IsFollowed;
            Comment = api.User.Comment;
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

            await _client.Api.AddUserFollowAsync(Id, visibility,
                await _client.CheckValidAccessToken().ConfigureAwait(false)).ConfigureAwait(false);

            IsFollowed = true;
        }

        public async Task UnfollowAsync(Visibility visibility = Visibility.Public)
        {
            if (!IsFollowed)
                throw new InvalidOperationException("The user has not been followed!");

            await _client.Api.DeleteUserFollowAsync(Id, visibility,
                await _client.CheckValidAccessToken().ConfigureAwait(false)).ConfigureAwait(false);

            IsFollowed = false;
        }
    }
}
