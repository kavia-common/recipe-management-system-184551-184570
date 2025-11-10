using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RecipeBackend.Services
{
    /// <summary>
    /// JWT token generation/validation using an in-memory signing key.
    /// </summary>
    public interface ITokenService
    {
        string IssueToken(string username);
        TokenValidationParameters GetValidationParameters();
        string Issuer { get; }
        string Audience { get; }
    }

    public class InMemoryTokenService : ITokenService
    {
        private readonly byte[] _key;
        public string Issuer { get; } = "RecipeBackend";
        public string Audience { get; } = "RecipeBackendClients";

        public InMemoryTokenService()
        {
            // Static signing key - acceptable for demo environments only.
            _key = Encoding.UTF8.GetBytes("SuperSecretRecipeBackendSigningKey_MinimalDemo_ChangeLater");
        }

        public string IssueToken(string username)
        {
            var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = handler.CreateJwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                subject: new ClaimsIdentity(claims),
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(8),
                issuedAt: DateTime.UtcNow,
                signingCredentials: creds
            );

            return handler.WriteToken(token);
        }

        public TokenValidationParameters GetValidationParameters() => new()
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    }
}
