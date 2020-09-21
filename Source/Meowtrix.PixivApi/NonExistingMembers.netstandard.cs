#if !NET5_0
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static Task<string> ReadAsStringAsync(this HttpContent httpContent, CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            return httpContent.ReadAsStringAsync();
        }

        public static Task<Stream> ReadAsStreamAsync(this HttpContent httpContent, CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            return httpContent.ReadAsStreamAsync();
        }
    }
}
#endif
