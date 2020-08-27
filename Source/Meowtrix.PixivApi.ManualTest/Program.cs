using System;
using System.Diagnostics;
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
                    Console.Write("Username:");
                    string username = Console.ReadLine()!;
                    Console.Write("Password:");
                    string password = Console.ReadLine()!;
                    response = (await client.AuthAsync(username, password)).authResponse;
                }
                authToken = response.AccessToken;
                Console.WriteLine($"Access token: {response.AccessToken}");
                Console.WriteLine($"Refresh token: {response.RefreshToken}");

                Debugger.Break();
            }

            Console.WriteLine("Begin user/detail");
            var userDetail = await client.GetUserDetailAsync(1113943, authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin user/illusts");
            var userIllusts = await client.GetUserIllustsAsync(1113943, authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin user/bookmarks/illust");
            var userBookmarksIllust = await client.GetUserBookmarkIllustsAsync(1113943, authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin illust/follow");
            var illustFollow = await client.GetIllustFollowAsync(authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin illust/comments");
            var illustComments = await client.GetIllustCommentsAsync(76995599, authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin illust/related");
            var illustRelated = await client.GetIllustRelatedAsync(76995599,
                seedIllustIds: new[] { 83492606, 82693472 },
                authToken: authToken);
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
            var search = await client.SearchIllustsAsync("女の子", authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin user/bookmark-tags/illust");
            var bookmarkTags = await client.GetUserBookmarkTagsIllustAsync(authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin user/following");
            var following = await client.GetUserFollowingsAsync(1113943, authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin user/follower");
            var follower = await client.GetUserFollowersAsync(1113943, authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin mypixiv");
            var mypixiv = await client.GetMyPixivUsersAsync(1113943, authToken: authToken);
            Debugger.Break();

            Console.WriteLine("Begin motion pic metadata");
            var motionMetadata = await client.GetMotionPicMetadataAsync(44340318, authToken: authToken);
            Debugger.Break();
        }
    }
}
