using System.Collections.Generic;
using System.Linq;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class UserInfoWithPreview : UserInfo, IConstructible<UserInfoWithPreview, UserPreview>
    {
        static UserInfoWithPreview IConstructible<UserInfoWithPreview, UserPreview>.Construct(PixivClient client, UserPreview api) => new(client, api);

        internal UserInfoWithPreview(PixivClient client, UserPreview api)
            : base(client, api.User)
            => PreviewIllusts = api.Illusts.Select(x => new Illust(client, x)).ToArray();

        public IReadOnlyList<Illust> PreviewIllusts { get; }
    }
}
