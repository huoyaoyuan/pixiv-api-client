using System;

namespace Meowtrix.PixivApi
{
    public enum UserIllustType
    {
        Illustrations,
        Comics,
    }

    internal static class UserIllustTypeExtensions
    {
        public static string ToQueryString(this UserIllustType type)
            => type switch
            {
                UserIllustType.Illustrations => "illust",
                UserIllustType.Comics => "manga",
                _ => throw new ArgumentException("Unknown illust type.", nameof(type))
            };
    }
}
