using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Authentication;

namespace Meowtrix.PixivApi.ManualTest
{
    internal sealed class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Please use debugger to inspect remote returned data.");
            Console.Write("Proxy:");
            string? proxy = Console.ReadLine();

            var tokenManager = new AccessTokenManager(null);
            using var client = new PixivApiClient(
                tokenManager,
                string.IsNullOrWhiteSpace(proxy)
                    ? null
                    : new HttpClientHandler { Proxy = new WebProxy($"http://{proxy}") });

            client.HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new(CultureInfo.CurrentCulture.Name));

            Console.Write("Saved refresh token:");
            string? refreshToken = Console.ReadLine();

            PixivAuthenticationResult response;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                response = await PixivAuthentication.AuthWithRefreshTokenAsync(new HttpMessageInvoker(client.InnerHandler, false), refreshToken);
            }
            else
            {
                var (codeVerify, url) = PixivAuthentication.PrepareWebLogin();
                Console.Write("Access this url in browser: ");
                Console.WriteLine(url);
                Console.Write("Paste the xxx part of pixiv://....?code=xxx (Use browser F12 to inspect it):");
                string code = Console.ReadLine()!;
                response = await PixivAuthentication.CompleteWebLoginAsync(new HttpMessageInvoker(client.InnerHandler, false), code, codeVerify);
                refreshToken = response.RefreshToken;
            }
            tokenManager.Authenticate(response);
            Console.WriteLine($"Access token: {response.AccessToken}");
            Console.WriteLine($"Refresh token: {response.RefreshToken}");

            Debugger.Break();

            {
                var hClient = new PixivClient(client);
                await hClient.LoginAsync(refreshToken);
            }

            Console.WriteLine("Begin user/detail");
            var userDetail = await client.GetUserDetailAsync(userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin user/illusts");
            var userIllusts = await client.GetUserIllustsAsync(userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin user/bookmarks/illust");
            var userBookmarksIllust = await client.GetUserBookmarkIllustsAsync(userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin illust/follow");
            var illustFollow = await client.GetIllustFollowAsync();
            Debugger.Break();

            Console.WriteLine("Begin illust/comments");
            var illustComments = await client.GetIllustCommentsAsync(illustId: 76995599);
            Debugger.Break();

            Console.WriteLine("Begin illust/related");
            var illustRelated = await client.GetIllustRelatedAsync(
                illustId: 76995599,
                seedIllustIds: [83492606, 82693472]);
            Debugger.Break();

            Console.WriteLine("Begin illust/ranking");
            var illustRanking = await client.GetIllustRankingAsync();
            Debugger.Break();

            Console.WriteLine("Begin trending-tags/illust");
            var trendingTags = await client.GetTrendingTagsIllustAsync();
            Debugger.Break();

            //Console.WriteLine("Adding bookmark");
            //await client.AddIllustBookmarkAsync(83492606);
            //Debugger.Break();

            //Console.WriteLine("Deleting bookmark");
            //await client.DeleteIllustBookmarkAsync(83492606);
            //Debugger.Break();

            Console.WriteLine("Begin search");
            var search = await client.SearchIllustsAsync(word: "女の子");
            Debugger.Break();

            Console.WriteLine("Begin user/bookmark-tags/illust");
            var bookmarkTags = await client.GetUserBookmarkTagsIllustAsync(userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin user/following");
            var following = await client.GetUserFollowingsAsync(userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin user/follower");
            var follower = await client.GetUserFollowersAsync(userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin mypixiv");
            var mypixiv = await client.GetMyPixivUsersAsync(userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin illust/detail");
            var illustDetail = await client.GetIllustDetailAsync(illustId: 44340318);
            Debugger.Break();

            Console.WriteLine("Begin motion pic metadata");
            var motionMetadata = await client.GetAnimatedPictureMetadataAsync(illustId: 84137005);
            Debugger.Break();
        }
    }
}
