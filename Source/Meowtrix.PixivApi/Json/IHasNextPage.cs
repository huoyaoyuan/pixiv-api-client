using System;

namespace Meowtrix.PixivApi.Json
{
    public interface IHasNextPage
    {
        Uri? NextUrl { get; }
    }
}
