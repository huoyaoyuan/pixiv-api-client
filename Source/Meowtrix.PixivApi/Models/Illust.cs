using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class Illust
    {
        private readonly PixivClient _client;

        internal Illust(PixivClient client, UserIllustPreview api)
        {
            _client = client;
            Id = api.Id;
            Title = api.Title;
#pragma warning disable CA1307 // 指定 StringComparison
            Description = api.Caption.Replace("<br/>", string.Empty);
#pragma warning restore CA1307 // 指定 StringComparison
            IsR18 = api.XRestrict > 0;
            Tags = api.Tags.Select(t => t.Name).ToArray();
            Created = api.CreateDate;
            SizePixels = new Size(api.Width, api.Height);
            TotalView = api.TotalView;
            TotalBookmarks = api.TotalBookmarks;
            IsBookmarked = api.IsBookmarked;

            if (api.PageCount == 1)
            {
                if (api.MetaSinglePage.OriginalImageUrl is null)
                    throw new InvalidOperationException("Corrupt api response");

                Pages = new[] { new IllustPage(_client, api.ImageUrls, api.MetaSinglePage.OriginalImageUrl) };
            }
            else
            {
                if (api.MetaPages.IsDefault || api.MetaPages.Length != api.PageCount)
                    throw new InvalidOperationException("Corrupt api response");

                Pages = api.MetaPages.Select(p => new IllustPage(_client, p.ImageUrls)).ToArray();
            }

            User = new UserInfo(client, api.User);
        }

        public int Id { get; }
        public string Title { get; }
        public string Description { get; }
        public bool IsR18 { get; }
        public IReadOnlyCollection<string> Tags { get; }
        public DateTimeOffset Created { get; }
        public Size SizePixels { get; }
        public int TotalView { get; }
        public int TotalBookmarks { get; }
        public bool IsBookmarked { get; private set; }

        public UserInfo User { get; }

        public async Task AddBookmarkAsync(Visibility visibility = Visibility.Public)
        {
            if (IsBookmarked)
                throw new InvalidOperationException("The illust is already bookmarked.");

            await _client.Api.AddIllustBookmarkAsync(Id, visibility,
                  authToken: await _client.CheckValidAccessToken().ConfigureAwait(false))
              .ConfigureAwait(false);

            IsBookmarked = true;
        }

        public async Task DeleteBookmarkAsync()
        {
            if (!IsBookmarked)
                throw new InvalidOperationException("There's no bookmark on this illust.");

            await _client.Api.DeleteIllustBookmarkAsync(Id,
                  authToken: await _client.CheckValidAccessToken().ConfigureAwait(false))
              .ConfigureAwait(false);

            IsBookmarked = false;
        }

        public IReadOnlyList<IllustPage> Pages { get; }

        public async IAsyncEnumerable<Comment> GetCommentsAsync()
        {
            var response = await _client.Api.GetIllustCommentsAsync(Id,
                authToken: await _client.CheckValidAccessToken().ConfigureAwait(false)).ConfigureAwait(false);

            while (response is not null)
            {
                foreach (var c in response.Comments)
                    yield return new Comment(_client, this, c);

                response = await _client.Api.GetNextPageAsync(response,
                    await _client.CheckValidAccessToken().ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        public async Task<Comment> PostCommentAsync(string content, Comment? parent = null)
        {
            var response = await _client.Api.PostIllustCommentAsync(Id, content, parent?.Id,
                await _client.CheckValidAccessToken().ConfigureAwait(false)).ConfigureAwait(false);

            return new Comment(_client, this, response.Comment);
        }
    }
}
