using System.IdentityModel.Tokens.Jwt;
using Claim = System.Security.Claims.Claim;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

internal sealed class PasswordChangeTokens
{
    public const string Username = "jdoe@test.local";
    private const string Audience = "portal-test";
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(new string('p', 64))) { KeyId = "password-change-test" };

    public TokenVerifier CreateVerifier(PortalSettings settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Constants.TOKEN_VALIDATION] = "{\"keys\":[{\"kty\":\"oct\",\"kid\":\"password-change-test\",\"k\":\""
                + Base64UrlEncoder.Encode(_key.Key) + "\"}]}",
            ["PortalSettings:MultiFactorApiSettings:ApiKey"] = Audience
        }).Build();
        return new TokenVerifier(config, settings, NullLogger<TokenVerifier>.Instance);
    }

    public string Create(bool mustChange, string? saml = "saml-session", string? oidc = null,
        string identity = Username, string requestId = "request-id")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identity), new(JwtRegisteredClaimNames.Jti, requestId),
            new(Constants.MultiFactorClaims.RawUserName, Username), new("amr", "pwd"),
            new("department", "engineering"), new("groups", "group-a"), new("groups", "group-b")
        };
        if (mustChange) claims.Add(new Claim(Constants.MultiFactorClaims.ChangePassword, "true"));
        if (saml != null) claims.Add(new Claim(Constants.MultiFactorClaims.SamlSessionId, saml));
        if (oidc != null) claims.Add(new Claim(Constants.MultiFactorClaims.OidcSessionId, oidc));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("test", Audience, claims,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5), new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)));
    }
}
