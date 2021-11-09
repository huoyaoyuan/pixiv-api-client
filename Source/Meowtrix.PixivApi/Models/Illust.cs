using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class Illust
    {
        private readonly PixivClient _client;

        internal Illust(PixivClient client, IllustDetail api)
        {
            _client = client;
            Id = api.Id;
            Title = api.Title;
#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1307 // 指定 StringComparison
            Description = api.Caption.Replace("<br/>", string.Empty);
#pragma warning restore CA1307 // 指定 StringComparison
#pragma warning restore IDE0079 // 请删除不必要的忽略
            IsR18 = api.XRestrict > 0;
            Tags = api.Tags.Select(t => new Tag(client, t)).ToArray();
            Created = api.CreateDate;
            SizePixels = new Size(api.Width, api.Height);
            TotalView = api.TotalView;
            TotalBookmarks = api.TotalBookmarks;
            IsBookmarked = api.IsBookmarked;

            if (api.PageCount == 1)
            {
                if (api.MetaSinglePage.OriginalImageUrl is null)
                    throw new InvalidOperationException("Corrupt api response");

                Pages = new[] { new IllustPage(this, 0, _client, api.ImageUrls, api.MetaSinglePage.OriginalImageUrl) };
            }
            else
            {
                if (api.MetaPages.IsDefault || api.MetaPages.Length != api.PageCount)
                    throw new InvalidOperationException("Corrupt api response");

                Pages = api.MetaPages.Select((p, i) => new IllustPage(this, i, _client, p.ImageUrls)).ToArray();
            }

            User = new UserInfo(client, api.User);
            IsAnimated = api.Type == "ugoira";

            SeriesId = api.Series?.Id switch
            {
                0 or null => null,
                var other => other
            };
        }

        public int Id { get; }
        public string Title { get; }
        public string Description { get; }
        public bool IsR18 { get; }
        public IReadOnlyCollection<Tag> Tags { get; }
        public DateTimeOffset Created { get; }
        public Size SizePixels { get; }
        public int TotalView { get; }
        public int TotalBookmarks { get; }
        public bool IsBookmarked { get; }

        public UserInfo User { get; }

        public IReadOnlyList<IllustPage> Pages { get; }

        public async IAsyncEnumerable<Comment> GetCommentsAsync([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            var response = await _client.Api.GetIllustCommentsAsync(authToken: await _client.CheckTokenAsync(),
                illustId: Id,
                cancellation: cancellation).ConfigureAwait(false);

            while (response is not null)
            {
                foreach (var c in response.Comments)
                    yield return new Comment(_client, this, c);

                response = await _client.Api.GetNextPageAsync(await _client.CheckTokenAsync(),
                    response,
                    cancellation).ConfigureAwait(false);
            }
        }

        public async Task<Comment> PostCommentAsync(string content, Comment? parent = null)
        {
            var response = await _client.Api.PostIllustCommentAsync(await _client.CheckTokenAsync(), Id, content,
                parent?.Id).ConfigureAwait(false);

            return new Comment(_client, this, response.Comment);
        }

        public bool IsAnimated { get; }

        public async Task<AnimatedPictureDetail> GetAnimatedDetailAsync(CancellationToken cancellation = default)
        {
            if (!IsAnimated)
                throw new InvalidOperationException("This illust is not an animated picture.");

            var response = await _client.Api.GetAnimatedPictureMetadataAsync(await _client.CheckTokenAsync(),
                Id,
                cancellation: cancellation).ConfigureAwait(false);

            return new AnimatedPictureDetail(_client, response);
        }

        public int? SeriesId { get; }

        [MemberNotNull(nameof(SeriesId))]
        public async Task<IllustSeries> GetSeriesAsync(CancellationToken cancellation = default)
        {
            return await _client.GetIllustSeriesAsync(
                SeriesId ?? throw new InvalidOperationException("The illust doesn't belongs to a series."),
                cancellation).ConfigureAwait(false);
        }
    }
}
