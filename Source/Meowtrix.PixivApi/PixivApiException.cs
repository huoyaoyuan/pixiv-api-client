using System;
using System.Runtime.Serialization;

namespace Meowtrix.PixivApi
{
    [Serializable]
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

        protected PixivApiException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }

    public sealed class PixivApiErrorMessage
    {
        public ErrorType? Error { get; init; }

        public record ErrorType(string? UserMessage, string? Message, string? Reason, object? UserMessageDetails);
    }
}
