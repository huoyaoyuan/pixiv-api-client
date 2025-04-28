#if !NET5_0_OR_GREATER
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static class HttpClientExtensions
    {
        public static Task<string> ReadAsStringAsync(this HttpContent httpContent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return httpContent.ReadAsStringAsync();
        }

        public static Task<Stream> ReadAsStreamAsync(this HttpContent httpContent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return httpContent.ReadAsStreamAsync();
        }

        public static async Task<string> GetStringAsync(this HttpClient httpClient, string uri, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static async Task<Stream> GetStreamAsync(this HttpClient httpClient, Uri uri, CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }
    }
}
#endif
