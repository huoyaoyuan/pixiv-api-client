using System;

namespace Meowtrix.PixivApi.Authentication
{
    public class AuthenticationFailedException(string? message, Exception? innerException)
        : Exception(message, innerException)
    {
    }
}
