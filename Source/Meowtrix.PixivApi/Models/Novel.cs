using System;
using System.Threading;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class Novel
    {
        private readonly PixivClient _client;

        internal Novel(PixivClient client, NovelDetail api)
        {
            _client = client;
            Id = api.Id;
            Title = api.Title;
            Description = api.Caption;
            IsR18 = api.XRestrict > 0;
            Created = api.CreateDate;
            TotalView = api.TotalView;
            TotalBookmarks = api.TotalBookmarks;
            IsBookmarked = api.IsBookmarked;
            PageCount = api.PageCount;
            TextLength = api.TextLength;

            User = new UserInfo(client, api.User);

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

        public DateTimeOffset Created { get; }
        public int PageCount { get; }
        public int TextLength { get; }
        public UserInfo User { get; }
        public int? SeriesId { get; }
        public int TotalView { get; }
        public int TotalBookmarks { get; }
        public bool IsBookmarked { get; }

        public async Task<string> GetTextAsync(CancellationToken cancellation = default)
            => (await _client.Api.GetNovelTextAsync(
                await _client.CheckTokenAsync(),
                Id,
                cancellation).ConfigureAwait(false)).NovelText;
    }
}
