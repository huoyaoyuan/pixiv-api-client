using System.Collections.Generic;
using System.Linq;
using Meowtrix.PixivApi.Json;

namespace Meowtrix.PixivApi.Models
{
    public class UserInfoWithPreview : UserInfo
    {
        internal UserInfoWithPreview(PixivClient client, UserPreview api)
            : base(client, api.User)
            => PreviewIllusts = api.Illusts.Select(x => new Illust(client, x)).ToArray();

        public IReadOnlyList<Illust> PreviewIllusts { get; }
    }
}
