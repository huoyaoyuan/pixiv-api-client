using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
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

        public Task<HttpResponseMessage> GetZipAsync(CancellationToken cancellation = default)
            => _client.Api.GetImageAsync(_zipUrl, cancellation);

        public async Task<ZipArchive> GetZipArchiveAsync(CancellationToken cancellation = default)
        {
            var response = (await GetZipAsync(cancellation).ConfigureAwait(false))
                .EnsureSuccessStatusCode();
            return new ZipArchive(await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false),
                ZipArchiveMode.Read);
        }

        public ImmutableArray<AnimatedPictureMetadata.Frame> Frames { get; }

        public async Task<IEnumerable<(Stream stream, TimeSpan frameTime)>> ExtractFramesAsync(
            CancellationToken cancellation = default)
        {
            return Extract(await GetZipArchiveAsync(cancellation).ConfigureAwait(false),
                cancellation);

            IEnumerable<(Stream stream, TimeSpan frameTime)> Extract(ZipArchive archive,
                CancellationToken cancellation = default)
            {
                using (archive)
                    foreach (var frame in Frames)
                    {
                        cancellation.ThrowIfCancellationRequested();

                        var stream = (archive.GetEntry(frame.File)
                            ?? throw new InvalidOperationException("Corrupt metadata."))
                            .Open();

                        yield return (stream, TimeSpan.FromMilliseconds(frame.Delay));
                    }
            }
        }
    }
}
