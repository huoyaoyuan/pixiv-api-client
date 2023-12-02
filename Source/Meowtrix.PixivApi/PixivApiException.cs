using System;

namespace Meowtrix.PixivApi
{
    public class PixivApiException : Exception
    {
        public string? OriginalMessage { get; }
        public PixivApiErrorMessage? Error { get; }

        public PixivApiException(string originalMessage, PixivApiErrorMessage? error, string message)
            : base(message)
        {
            OriginalMessage = originalMessage;
            Error = error;
        }
    }

    public sealed class PixivApiErrorMessage
    {
        public ErrorType? Error { get; init; }

        public record ErrorType(string? UserMessage, string? Message, string? Reason, object? UserMessageDetails);
    }
}
