namespace Meowtrix.PixivApi.Models;

internal interface IConstructible<TSelf, TApi>
{
    static abstract TSelf Construct(PixivClient client, TApi api);
}
