using System;
using System.Net.Http;
using System.Web;
using System.Windows;
using Meowtrix.PixivApi.Authentication;

namespace Meowtrix.PixivApi.ManualLogin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            (string codeVerify, string loginUrl) = PixivAuthentication.PrepareWebLogin();
            WebView.Source = new(loginUrl);
            WebView.NavigationStarting += async (s, e) =>
            {
                if (e.Uri.StartsWith("pixiv:", StringComparison.Ordinal))
                {
                    e.Cancel = true;
                    try
                    {
                        var uri = new Uri(e.Uri);
                        var query = HttpUtility.ParseQueryString(uri.Query);
                        var loginResult = await PixivAuthentication.CompleteWebLoginAsync(
                            new HttpClient(),
                            query["code"]!,
                            codeVerify)
                            .ConfigureAwait(true);
                        Text.Text = $"Your refresh token is: \n\n{loginResult.RefreshToken}\n\nDO NOT share it -- it's valid for a long term.";
                    }
                    catch (Exception ex)
                    {
                        Text.Text = ex.ToString();
                    }
                }
            };
        }
    }
}
