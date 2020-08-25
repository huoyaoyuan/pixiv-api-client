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

                Response response;
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    response = (await client.AuthAsync(refreshToken)).authResponse.Response;
                }
                else
                {
                    Console.Write("Username:");
                    string username = Console.ReadLine()!;
                    Console.Write("Password:");
                    string password = Console.ReadLine()!;
                    response = (await client.AuthAsync(username, password)).authResponse.Response;
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
        }
    }
}
