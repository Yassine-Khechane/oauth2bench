using IdentityModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace WebMVC.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.InternalLog = GetLog();
            return View();
        }

        public async Task<ActionResult> About()
        {
            ViewBag.Message = "Your application description page.";

            var user = User as ClaimsPrincipal;

            var token = user?.FindFirst("access_token")?.Value;
            var subject = user?.FindFirst("sub")?.Value ?? "<no subject>";

            if (token != null)
            {
                var client = new HttpClient();
                client.SetBearerToken(token);

                var result = await client.GetStringAsync("http://webapi.loc/Customer");
                ViewBag.Json = JObject.Parse(result.ToString()).ToString(Formatting.Indented);
            }

            ViewBag.Subject = subject;
            ViewBag.InternalLog = GetLog();
            return View();
        }

        [Authorize]
        public ActionResult Contact()
        {
            ViewBag.Message = "Claims";

            var user = User as ClaimsPrincipal;
            var token = user.FindFirst("access_token");

            if (token != null)
            {
                ViewBag.Token = token.Value;
            }

            ViewBag.InternalLog = GetLog();
            return View();
        }

        public ActionResult FromApp(string access_token)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            var tok1=tokenHandler.ReadToken(access_token) as JwtSecurityToken;
            SecurityToken tok2=null;

            ClaimsPrincipal user = null;

            try {
                user=tokenHandler.ValidateToken(access_token, new TokenValidationParameters {
                    ValidIssuer = "https://identity.loc/core",
                    ValidAudience = "https://identity.loc/core/resources",
                    IssuerSigningToken = new X509SecurityToken(new X509Certificate2(Base64Url.Decode("MIIDBTCCAfGgAwIBAgIQNQb+T2ncIrNA6cKvUA1GWTAJBgUrDgMCHQUAMBIxEDAOBgNVBAMTB0RldlJvb3QwHhcNMTAwMTIwMjIwMDAwWhcNMjAwMTIwMjIwMDAwWjAVMRMwEQYDVQQDEwppZHNydjN0ZXN0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqnTksBdxOiOlsmRNd+mMS2M3o1IDpK4uAr0T4/YqO3zYHAGAWTwsq4ms+NWynqY5HaB4EThNxuq2GWC5JKpO1YirOrwS97B5x9LJyHXPsdJcSikEI9BxOkl6WLQ0UzPxHdYTLpR4/O+0ILAlXw8NU4+jB4AP8Sn9YGYJ5w0fLw5YmWioXeWvocz1wHrZdJPxS8XnqHXwMUozVzQj+x6daOv5FmrHU1r9/bbp0a1GLv4BbTtSh4kMyz1hXylho0EvPg5p9YIKStbNAW9eNWvv5R8HN7PPei21AsUqxekK0oW9jnEdHewckToX7x5zULWKwwZIksll0XnVczVgy7fCFwIDAQABo1wwWjATBgNVHSUEDDAKBggrBgEFBQcDATBDBgNVHQEEPDA6gBDSFgDaV+Q2d2191r6A38tBoRQwEjEQMA4GA1UEAxMHRGV2Um9vdIIQLFk7exPNg41NRNaeNu0I9jAJBgUrDgMCHQUAA4IBAQBUnMSZxY5xosMEW6Mz4WEAjNoNv2QvqNmk23RMZGMgr516ROeWS5D3RlTNyU8FkstNCC4maDM3E0Bi4bbzW3AwrpbluqtcyMN3Pivqdxx+zKWKiORJqqLIvN8CT1fVPxxXb/e9GOdaR8eXSmB0PgNUhM4IjgNkwBbvWC9F/lzvwjlQgciR7d4GfXPYsE1vf8tmdQaY8/PtdAkExmbrb9MihdggSoGXlELrPA91Yce+fiRcKY3rQlNWVd4DOoJ/cPXsXwry8pWjNCo5JD8Q+RQ5yZEy7YPoifwemLhTdsBz3hlZr28oCGJ3kbnpW0xGvQb3VHSTVVbeei0CfXoW6iz1"))),
                }, out tok2);
                if (tok2.ValidTo < DateTime.UtcNow) throw new Exception("Token expired");
                if (tok2.ValidFrom > DateTime.UtcNow) throw new Exception("Token not yet valid");
                ViewBag.ValidationError = null;
            } catch (Exception e)
            {
                user = null;
                ViewBag.ValidationError = e.Message;
            }

            var jsToken=JsonConvert.SerializeObject(tok2,Formatting.Indented);

            ViewBag.ClaimsPrincipal = user;
            ViewBag.Token = access_token;
            ViewBag.Message = "Decoded access token";
            ViewBag.JsToken = jsToken;

            ViewBag.InternalLog = GetLog();
            return View();
        }

        public List<string> GetLog()
        {
            var il = HttpContext.GetOwinContext().Get<InternalLogger>("InternalLogger");
            return il.RetrieveOutput();
        }
    }
}