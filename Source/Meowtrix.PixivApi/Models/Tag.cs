﻿using System.Collections.Generic;
using System.Threading;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class Tag
    {
        private readonly PixivClient _client;

        public Tag(PixivClient client, IllustTag api)
        {
            _client = client;
            Name = api.Name;
            TranslatedName = api.TranslatedName;
        }

        public string Name { get; }
        public string? TranslatedName { get; }

        public IAsyncEnumerable<Illust> GetIllustsAsync(IllustFilterOptions? options = null,
            CancellationToken cancellation = default)
            => _client.SearchIllustsAsync(Name, IllustSearchTarget.ExactTag, options, cancellation);
    }
}
