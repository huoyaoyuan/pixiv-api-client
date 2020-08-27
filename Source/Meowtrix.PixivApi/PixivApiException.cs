using System;
using System.Runtime.Serialization;

namespace Meowtrix.PixivApi
{
    [Serializable]
    public class PixivApiException : Exception
    {
        public string? OriginalMessage { get; }
        public PixivErrorMessage? Error { get; }

        public PixivApiException(string originalMessage, PixivErrorMessage? error, string message)
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

#pragma warning disable IDE1006 // Naming style

    public sealed class PixivErrorMessage
    {
        public bool HasError { get; init; }
        public string? Error { get; init; }
        public ErrorsType? Errors { get; init; }


        public sealed record ErrorsType(SystemError? System);

        public sealed record SystemError(int Code, string? Message);
    }
}
