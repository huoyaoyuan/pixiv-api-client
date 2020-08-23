using System;

namespace Meowtrix.PixivApi.Json
{
    [GenerateNullTest]
    public class AuthResult
    {
        public Response? Response { get; set; }
    }

    public class Response
    {
        public string? AccessToken { get; set; }
        public long ExpiresIn { get; set; }
        public string? TokenType { get; set; }
        public string? Scope { get; set; }
        public string? RefreshToken { get; set; }
        public AuthUser? User { get; set; }
        public string? DeviceToken { get; set; }
    }

    public class AuthUser
    {
        public ProfileImageUrls? ProfileImageUrls { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Account { get; set; }
        public string? MailAddress { get; set; }
        public bool IsPremium { get; set; }
        public long XRestrict { get; set; }
        public bool IsMainAuthorized { get; set; }
    }

    public class ProfileImageUrls
    {
        public Uri? Px16x16 { get; set; }
        public Uri? Px50x50 { get; set; }
        public Uri? Px170x170 { get; set; }
    }
}
