using System;

namespace Meowtrix.PixivApi
{
    public enum IllustSearchTarget
    {
        PartialTag,
        ExactTag,
        TitleCaption,
    }

    internal static class IllustSearchTargetExtensions
    {
        public static string ToQueryString(this IllustSearchTarget target)
            => target switch
            {
                IllustSearchTarget.PartialTag => "partial_match_for_tags",
                IllustSearchTarget.ExactTag => "exact_match_for_tags",
                IllustSearchTarget.TitleCaption => "title_and_caption",
                _ => throw new ArgumentException("Unknown search target", nameof(target))
            };
    }
}
