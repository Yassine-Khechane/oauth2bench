using System;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using System.Collections.Generic;
using Configuration;
using System.Linq;

namespace IdentityServer
{
    internal class CustomClientStore : IClientStore
    {
        private const string AppPrefix = "NativeApp$";
        static List<Client> _client = Clients.Get();

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            if (clientId.StartsWith(AppPrefix, StringComparison.Ordinal))
            {
                var puid = clientId.Substring(AppPrefix.Length).Trim();

                return new Client
                {
                    ClientName = $"Native application {puid}",
                    Enabled = true,
                    ClientId = clientId,
                    ClientSecrets = new List<Secret>
                    {
                        new Secret(puid.Sha256())
                    },

                    Flow = Flows.ResourceOwner,

                    AllowedScopes = new List<string>
                    {
                        "read",
                        "write",
                        "offline_access",
                    },

                    AccessTokenType = AccessTokenType.Jwt,
                    AccessTokenLifetime = 600,
                    AbsoluteRefreshTokenLifetime = 86400,
                    SlidingRefreshTokenLifetime = 24*60*60*30,

                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Sliding
                };
                
            }

            return _client.FirstOrDefault(x => x.ClientId.Equals(clientId, StringComparison.Ordinal));

        }
    }
}