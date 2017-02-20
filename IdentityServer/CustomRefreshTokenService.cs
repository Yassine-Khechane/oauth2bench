using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;

namespace IdentityServer
{
    internal class CustomRefreshTokenService : IRefreshTokenService
    {
        public Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, Token accessToken, Client client)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, Client client)
        {
            throw new NotImplementedException();
        }
    }
}