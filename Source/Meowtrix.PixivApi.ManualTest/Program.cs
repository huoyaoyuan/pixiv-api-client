using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Meowtrix.PixivApi.Json;

#pragma warning disable CA2007

namespace Meowtrix.PixivApi.ManualTest
{
    internal class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Please use debugger to inspect remote returned data.");
            Console.Write("Proxy:");
            string? proxy = Console.ReadLine();

            using var client = string.IsNullOrWhiteSpace(proxy)
                ? new PixivApiClient()
                : new PixivApiClient(new WebProxy($"http://{proxy}"));

            client.DefaultRequestHeaders.AcceptLanguage.Add(new(CultureInfo.CurrentCulture.Name));

            Console.Write("Saved access token:");
            string? authToken = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(authToken))
            {
                Console.Write("Saved refresh token:");
                string? refreshToken = Console.ReadLine();

                AuthResponse response;
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    response = (await client.AuthAsync(refreshToken)).authResponse;
                }
                else
                {
                    var (codeVerify, url) = client.BeginAuth();
                    Console.Write("Access this url in browser: ");
                    Console.WriteLine(url);
                    Console.Write("Paste the xxx part of pixiv://....?code=xxx (Use browser F12 to inspect it):");
                    string code = Console.ReadLine()!;
                    response = (await client.CompleteAuthAsync(code, codeVerify)).authResponse;
                }
                authToken = response.AccessToken;
                Console.WriteLine($"Access token: {response.AccessToken}");
                Console.WriteLine($"Refresh token: {response.RefreshToken}");

                Debugger.Break();
            }

            Console.WriteLine("Begin user/detail");
            var userDetail = await client.GetUserDetailAsync(authToken: authToken, userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin user/illusts");
            var userIllusts = await client.GetUserIllustsAsync(authToken: authToken, userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin user/bookmarks/illust");
            var userBookmarksIllust = await client.GetUserBookmarkIllustsAsync(authToken: authToken, userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin illust/follow");
            var illustFollow = await client.GetIllustFollowAsync(authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin illust/comments");
            var illustComments = await client.GetIllustCommentsAsync(authToken: authToken, illustId: 76995599);
            Debugger.Break();

            Console.WriteLine("Begin illust/related");
            var illustRelated = await client.GetIllustRelatedAsync(authToken: authToken,
                illustId: 76995599,
                seedIllustIds: new[] { 83492606, 82693472 });
            Debugger.Break();

            Console.WriteLine("Begin illust/ranking");
            var illustRanking = await client.GetIllustRankingAsync(authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin trending-tags/illust");
            var trendingTags = await client.GetTrendingTagsIllustAsync(authToken: authToken);
            Debugger.Break();

            //Console.WriteLine("Adding bookmark");
            //await client.AddIllustBookmarkAsync(83492606, authToken: authToken);
            //Debugger.Break();

            //Console.WriteLine("Deleting bookmark");
            //await client.DeleteIllustBookmarkAsync(83492606, authToken: authToken);
            //Debugger.Break();

            Console.WriteLine("Begin search");
            var search = await client.SearchIllustsAsync(authToken: authToken, word: "女の子");
            Debugger.Break();

            Console.WriteLine("Begin user/bookmark-tags/illust");
            var bookmarkTags = await client.GetUserBookmarkTagsIllustAsync(authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin user/following");
            var following = await client.GetUserFollowingsAsync(authToken: authToken, userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin user/follower");
            var follower = await client.GetUserFollowersAsync(authToken: authToken, userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin mypixiv");
            var mypixiv = await client.GetMyPixivUsersAsync(authToken: authToken, userId: 1113943);
            Debugger.Break();

            Console.WriteLine("Begin illust/detail");
            var illustDetail = await client.GetIllustDetailAsync(authToken: authToken, illustId: 44340318);
            Debugger.Break();

            Console.WriteLine("Begin motion pic metadata");
            var motionMetadata = await client.GetAnimatedPictureMetadataAsync(authToken: authToken, illustId: 84137005);
            Debugger.Break();
        }
    }
}
