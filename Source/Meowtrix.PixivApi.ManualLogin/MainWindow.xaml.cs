using System;
using System.Threading.Tasks;
using System.Windows;

namespace Meowtrix.PixivApi.ManualLogin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PixivClient _client = new();

        public MainWindow() => InitializeComponent();

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string refreshToken = await _client.LoginAsync(uri =>
            {
                var tcs = new TaskCompletionSource<Uri>();
                WebView.Source = new(uri);
                WebView.NavigationStarting += (s, e) =>
                {
                    if (e.Uri.StartsWith("pixiv:", StringComparison.Ordinal))
                    {
                        e.Cancel = true;
                        tcs.SetResult(new(e.Uri));
                    }
                };

                return tcs.Task;
            }).ConfigureAwait(true);

            Text.Text = $"Your refresh token is: \n\n{refreshToken}\n\nDO NOT share it -- it's valid for a long term.";
        }
    }
}
