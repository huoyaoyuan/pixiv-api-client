using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            return Client.ToIllustAsyncEnumerable(c
                => Client.Api.GetUserIllustsAsync(userId: Id, illustType: illustType, cancellationToken: c),
                cancellation);
        }

        public IAsyncEnumerable<Illust> GetBookmarksAsync(CancellationToken cancellation = default)
        {
            return Client.ToIllustAsyncEnumerable(c
                => Client.Api.GetUserBookmarkIllustsAsync(userId: Id, cancellationToken: c),
                cancellation);
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetFollowingUsersAsync(CancellationToken cancellation = default)
        {
            return Client.ToUserAsyncEnumerable(c
                => Client.Api.GetUserFollowingsAsync(userId: Id, restrict: Visibility.Public, cancellationToken: c),
                cancellation);
        }

        public IAsyncEnumerable<UserInfoWithPreview> GetFollowerUsersAsync(CancellationToken cancellation = default)
        {
            return Client.ToUserAsyncEnumerable(c
                => Client.Api.GetUserFollowersAsync(userId: Id, restrict: Visibility.Public, cancellationToken: c),
                cancellation);
        }

        public async IAsyncEnumerable<IllustSeries> GetIllustSeriesAsync([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            var rawEnumerable = Client.ToAsyncEnumerable<UserIllustSeries, IllustSeriesDetails>(c
                => Client.Api.GetUserIllustSeriesAsync(Id, c),
                cancellation);

            await foreach (var raw in rawEnumerable)
                yield return new(Client, raw);
        }

        public async IAsyncEnumerable<Novel> GetNovelsAsync([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            var rawEnumerable = Client.ToAsyncEnumerable<UserNovels, NovelDetail>(c
                => Client.Api.GetUserNovelsAsync(Id, c),
                cancellation);

            await foreach (var raw in rawEnumerable)
                yield return new(Client, raw);
        }
    }
}
