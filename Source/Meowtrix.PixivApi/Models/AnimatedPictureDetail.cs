using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class AnimatedPictureDetail
    {
        private readonly PixivClient _client;
        private readonly Uri _zipUrl;

        public AnimatedPictureDetail(PixivClient client, AnimatedPictureMetadata api)
        {
            _client = client;
            _zipUrl = api.UgoiraMetadata.ZipUrls.Medium;
            Frames = api.UgoiraMetadata.Frames;
        }

        public Task<HttpResponseMessage> GetZipAsync()
            => _client.Api.GetImageAsync(_zipUrl);

        public async Task<ZipArchive> GetZipArchiveAsync()
        {
            var response = (await GetZipAsync().ConfigureAwait(false))
                .EnsureSuccessStatusCode();
            return new ZipArchive(await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                ZipArchiveMode.Read);
        }

        public ImmutableArray<AnimatedPictureMetadata.Frame> Frames { get; }

        public async Task<IEnumerable<(Stream stream, TimeSpan frameTime)>> ExtractFramesAsync()
        {
            return Extract(await GetZipArchiveAsync().ConfigureAwait(false));

            IEnumerable<(Stream stream, TimeSpan frameTime)> Extract(ZipArchive archive)
            {
                using (archive)
                    foreach (var frame in Frames)
                    {
                        var stream = (archive.GetEntry(frame.File)
                            ?? throw new InvalidOperationException("Corrupt metadata."))
                            .Open();

                        yield return (stream, TimeSpan.FromMilliseconds(frame.Delay));
                    }
            }
        }
    }
}
