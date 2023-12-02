using System;

namespace Meowtrix.PixivApi
{
    public class PixivAuthException : Exception
    {
        public string? OriginalMessage { get; }
        public PixivAuthErrorMessage? Error { get; }

        public PixivAuthException(string originalMessage, PixivAuthErrorMessage? error, string message)
            : base(message)
        {
            OriginalMessage = originalMessage;
            Error = error;
        }
    }

    public sealed class PixivAuthErrorMessage
    {
        public bool HasError { get; init; }
        public string? Error { get; init; }
        public ErrorsType? Errors { get; init; }


        public sealed record ErrorsType(SystemError? System);

        public sealed record SystemError(int Code, string? Message);
    }
}
