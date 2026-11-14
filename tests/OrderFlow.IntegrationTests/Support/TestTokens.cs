using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace OrderFlow.IntegrationTests.Support;

/// <summary>
/// Issues JWTs for the tests with a symmetric key known only to them, so the tests never depend on
/// dotnet user-jwts, the developer's user secrets or an identity provider.
/// </summary>
internal static class TestTokens
{
    public const string Issuer = "orderflow-tests";
    public const string Audience = "orderflow-api";

    // HS256 needs at least 256 bits (32 bytes) of key material.
    public static readonly SymmetricSecurityKey Key =
        new(Encoding.UTF8.GetBytes("orderflow-integration-tests-signing-key-0123456789"));

    public static string Create(string subject, string name, params string[] roles)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = subject,
                [JwtRegisteredClaimNames.UniqueName] = name,
                ["role"] = roles
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}