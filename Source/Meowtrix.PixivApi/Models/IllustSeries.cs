using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class IllustSeries
    {
        private readonly PixivClient _client;
        private readonly Uri _coverUri;

        internal IllustSeries(PixivClient client, IllustSeriesDetails api)
        {
            _client = client;
            Id = api.Id;
            Name = api.Title;
            Description = api.Caption;
            _coverUri = api.CoverImageUrls.Medium;
            Created = api.CreateDate;
            WorksCount = api.SeriesWorkCount;
            Height = api.Height;
            Width = api.Width;
        }

        public ImageInfo Cover => new(_coverUri, _client);

        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int WorksCount { get; }
        public DateTimeOffset Created { get; }
        public int Height { get; }
        public int Width { get; }

        public IAsyncEnumerable<Illust> GetIllustsAsync(CancellationToken cancellation)
        {
            return _client.Api.EnumeratePagesAsync(
                _client.Api.GetIllustSeriesAsync(Id, cancellation), cancellation)
                .SelectMany(x => x.Illusts, (_, x) => new Illust(_client, x));
        }
    }
}
