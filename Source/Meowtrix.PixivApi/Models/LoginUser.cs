using System;
using System.Globalization;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class LoginUser
    {
        internal LoginUser(PixivClient client, AuthUser api)
        {
            Id = int.Parse(api.Id, NumberFormatInfo.InvariantInfo);
            Name = api.Name;
            Account = api.Account;
            MailAddress = api.MailAddress;
            IsPremium = api.IsPremium;
            IsR18Enabled = api.XRestrict > 0;
            _avatarUri = api.ProfileImageUrls.PixelSize170;
            _client = client;
        }

        public int Id { get; }
        public string Name { get; }
        public string Account { get; }
        public string MailAddress { get; }
        public bool IsPremium { get; }
        public bool IsR18Enabled { get; }

        private readonly Uri _avatarUri;
        private readonly PixivClient _client;

        public ImageInfo Avatar => new ImageInfo(_avatarUri, _client.Api);
    }
}
