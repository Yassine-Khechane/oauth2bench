using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NativeApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ApiCaller apiCaller;

        public MainPage()
        {
            this.InitializeComponent();
            apiCaller = new ApiCaller();
            apiCaller.OnNewTokenResponse = (resp,token) =>
            {
                var txt = resp!=null?(resp.Json.ToString(Newtonsoft.Json.Formatting.Indented) + "\n\n"):"";
                txt += Helpers.ParseAccessToken(token);

                textBlockTokenResult.Text = txt;
            };
        }

        private async void btnAuthenticate_Click(object sender, RoutedEventArgs e)
        {
            await apiCaller.Authenticate(textLogin.Text, textPassword.Text);
        }

        private async void btnCallApi_Click(object sender, RoutedEventArgs e)
        {
            try {
                var a = await apiCaller.GetItem<CustomerInfo>("http://webapi.loc:80/Customer");

                textBlockApiResult.Text = JsonConvert.SerializeObject(a,Formatting.Indented);
                
            }
            catch (Exception ex)
            {
                textBlockApiResult.Text = ex.Message;
            }
        }

        private async void btnAuthorize_Click(object sender, RoutedEventArgs e)
        {
            btnAuthorize.Visibility = Visibility.Collapsed;
            btnCancelAuthorize.Visibility = Visibility.Visible;

            var b =await apiCaller.Authorize(webView);

            btnAuthorize.Visibility = Visibility.Visible;
            btnCancelAuthorize.Visibility = Visibility.Collapsed;

            /*var areq = new AuthorizeRequest("https://identity.loc/core/connect/authorize");
            var uri=areq.CreateAuthorizeUrl(clientId: "codeclient", responseType: "code", scope: "read write offline_access", 
                redirectUri: "https://identity.loc/winrthelper", 
                acrValues: $"puid:{_puid}");

            webView.Navigate(new Uri(uri));
            */
            /*var result = await WebAuthenticationBroker.AuthenticateAsync(
                WebAuthenticationOptions.UseHttpPost,
                new Uri(uri));*/

        }

        private void btnCancelAuthorize_Click(object sender, RoutedEventArgs e)
        {
            apiCaller.CancelPendingAuthorize();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            webView.Navigate(new Uri($"http://webmvc.loc/Home/Contact?access_token={apiCaller.AccessToken}"));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await apiCaller.Initialize();
        }

        /*private void webView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.ToString().StartsWith("https://authorize-return/", StringComparison.Ordinal))
            {
                textBlockTokenResult.Text = args.Uri.ToString();
                args.Cancel = true;
                webView.NavigateToString(args.Uri.ToString());
            }
        }*/
        /*
        private void webView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            textBlockTokenResult.Text = e.CallingUri.ToString();
            webView.NavigateToString(e.CallingUri.ToString());
        }

        private async void webView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            apiCaller.AuthorizationCallback(args.Uri);
        }*/

    }

    public class CustomerInfo
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EMail { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string LoyaltyCardNumber { get; set; }
    }

    public static class Helpers
    {
        public static string ParseAccessToken(string token)
        {
            if (token == null) return null;
            var parts = token.Split('.');

            return string.Join("\n",
                        parts.Take(2)
                             .Select(x => Base64Url.Decode(x))
                             .Select(x => JObject.Parse(Encoding.UTF8.GetString(x, 0, x.Length)).ToString(Formatting.Indented))
                    );
        }

    }


}
