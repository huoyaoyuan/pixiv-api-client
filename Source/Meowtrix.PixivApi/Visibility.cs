using System;

namespace Meowtrix.PixivApi
{
    public enum Visibility
    {
        Public,
        Private,
    }

    internal static class VisibilityExtensions
    {
        public static string ToQueryString(this Visibility visibility)
            => visibility switch
            {
                Visibility.Private => "private",
                Visibility.Public => "public",
                _ => throw new ArgumentException("Unknown enum value.", nameof(visibility))
            };
    }
}
