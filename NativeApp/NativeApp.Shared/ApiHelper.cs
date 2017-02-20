using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace NativeApp
{
    public class TokenStorageItem
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public bool TokenGrantedByCode { get; set; }
    }

    public class ApiCaller
    {
        TokenResponse _tokenResponseField;
        TokenResponse _tokenResponse
        {
            get { return _tokenResponseField; }
            set
            {
                _tokenResponseField = value;
                if (value != null) _token = new TokenStorageItem { AccessToken = value.AccessToken, RefreshToken = value.RefreshToken };
            }
        }

        TokenStorageItem _token;

        string _puid;

        string _baseAddress = "https://identity.loc/core";
        string _authorizeHelperAddress = "https://identity.loc/winrthelper";

        TokenClient getAuthenticateTokenClient()=>new TokenClient(_baseAddress + "/connect/token", $"NativeApp${_puid}", _puid);
        TokenClient getAuthorizeTokenClient()=>new TokenClient(_baseAddress + "/connect/token", $"codeclient", "secret");

        TokenClient getRefreshTokenClient()
        {
            if (_token.TokenGrantedByCode)
                return getAuthenticateTokenClient();
            return getAuthorizeTokenClient();
        }

        public string AccessToken { get { return _token.AccessToken; } }

        public ApiCaller()
        {
            _puid = Base64Url.Encode(Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null).Id.ToArray());
        }

        public async Task Initialize()
        {
            await Task.Factory.StartNew(() =>
            {
                var t = RetrieveProtect<TokenStorageItem>("token");
                t.Wait();
                _token = t.Result;
            });

            if (OnNewTokenResponse != null && _token!=null)
                OnNewTokenResponse(null,_token.AccessToken);

        }

        public Action<TokenResponse,string> OnNewTokenResponse { get; set; }

        public async Task<bool> Authenticate(string login, string password)
        {
            var _tokenClient = getAuthenticateTokenClient();

            var resp = await _tokenClient.RequestResourceOwnerPasswordAsync(login, password, "read write offline_access", new { acr_values = $"puid:{_puid}" });

            if (OnNewTokenResponse != null)
                OnNewTokenResponse(resp,resp.AccessToken);

            _tokenResponse = resp;
            _token.TokenGrantedByCode = true;
            StoreProtect("token", _token);

            return resp!=null && !resp.IsError;
        }

        AutoResetEvent evt = new AutoResetEvent(false);

        public async Task<bool> Authorize(WebView webView)
        {
            var areq = new AuthorizeRequest(_baseAddress+"/connect/authorize");
            var uri = areq.CreateAuthorizeUrl(clientId: "codeclient", responseType: "code", scope: "read write offline_access",
                redirectUri: _authorizeHelperAddress,
                acrValues: $"puid:{_puid}");

            _tokenResponse = null;
            evt.Reset();

            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.Navigate(new Uri(uri));

            await Task.Run(() => evt.WaitOne());

            webView.NavigateToString("");
                        StoreProtect("token", _token);

            if (_tokenResponse==null || _tokenResponse.IsError)
                return false;
            return true;

        }

        public void CancelPendingAuthorize()
        {
            evt.Set();
        }

        private async void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (await AuthorizationCallback(args.Uri))
            {
                sender.NavigationCompleted -= WebView_NavigationCompleted;
                evt.Set();
            }
        }

        static Regex rx_code = new Regex(@"\?code=(?<code>[0-9A-Za-z]+)");
        static Regex rx_error = new Regex(@"\?error=(?<error>[^&]+)");

        private async Task<bool> AuthorizationCallback(Uri uri)
        {
            if (uri!=null && uri.ToString().StartsWith(_authorizeHelperAddress, StringComparison.Ordinal))
            {
                var q = uri.Query;
                var m = rx_code.Match(q);
                if (m.Success)
                {
                    var code = m.Groups["code"].Value;

                    var client = getAuthorizeTokenClient();

                    var resp = await client.RequestAuthorizationCodeAsync(
                        code, _authorizeHelperAddress);

                    if (OnNewTokenResponse != null)
                        OnNewTokenResponse(resp,resp.AccessToken);

                    _tokenResponse = resp;
                    _token.TokenGrantedByCode = false;

                    return true;
                }

                m = rx_error.Match(q);
                if (m.Success)
                {
                    _tokenResponse = null;
                    return true;
                }
            }
            return false;
        }

        public async Task<T> GetItem<T>(string someUri, bool refresh = false)
        {
            var rootFilter = new HttpBaseProtocolFilter();
            rootFilter.CacheControl.ReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;
            rootFilter.CacheControl.WriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior.NoCache;

            if (_token.AccessToken==null)
                return default(T);

            if (refresh)
            {
                var resp = await getRefreshTokenClient().RequestRefreshTokenAsync(_token.RefreshToken);
                if (OnNewTokenResponse != null)
                    OnNewTokenResponse(resp,resp.AccessToken);
                if (!resp.IsError)
                {
                    _tokenResponse = resp;
                    StoreProtect("token", _token);
                }
            }

            var httpclient = new HttpClient(rootFilter);
            httpclient.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", _token.AccessToken);
            HttpResponseMessage response = await httpclient.GetAsync(new Uri(someUri + "?" + DateTime.UtcNow.Ticks.ToString()));
            if (response.StatusCode == HttpStatusCode.Ok)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                // parse to json
                T resultItem = JsonConvert.DeserializeObject<T>(responseString);
                return resultItem;
            }
            else
            {
                if (!refresh)
                    return await GetItem<T>(someUri, true);
                throw new Exception((int)response.StatusCode + " " + response.ReasonPhrase);
            }

        }

        public async void StoreProtect<T>(            
            string strDescriptor, T obj)
        {

            var resultItem = JsonConvert.SerializeObject(obj);
                        
            // Encode the plaintext input message to a buffer.
            IBuffer buffMsg = CryptographicBuffer.ConvertStringToBinary(resultItem, BinaryStringEncoding.Utf8);

            // Create a DataProtectionProvider object for the specified descriptor.
            DataProtectionProvider Provider = new DataProtectionProvider("LOCAL=user");

            // Encrypt the message.
            IBuffer buffProtected = await Provider.ProtectAsync(buffMsg);
            // Execution of the SampleProtectAsync function resumes here
            // after the awaited task (Provider.ProtectAsync) completes.

            ApplicationData.Current.LocalSettings.Values[strDescriptor] = buffProtected.ToArray();
        }
        public async Task<T> RetrieveProtect<T>(
            string strDescriptor)
        {

            object obj;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(strDescriptor, out obj))
            {
                // Create a DataProtectionProvider object for the specified descriptor.
                DataProtectionProvider Provider = new DataProtectionProvider("LOCAL=user");

                var res = await Provider.UnprotectAsync(((byte[])obj).AsBuffer());

                var json = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, res);

                return JsonConvert.DeserializeObject<T>(json);

            }
            return default(T);
        }
    }
}
