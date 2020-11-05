using System;
using System.Collections.Immutable;

namespace Meowtrix.PixivApi.Json
{
    public interface IHasNextPage
    {
        Uri? NextUrl { get; }
    }

    public interface IHasNextPage<T> : IHasNextPage
    {
        ImmutableArray<T> Items { get; }
    }
}
