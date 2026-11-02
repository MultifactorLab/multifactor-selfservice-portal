using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LdapForNet;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core;
using MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeExpiredPassword;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;
using static LdapForNet.Native.Native;
using Claim = System.Security.Claims.Claim;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Stories;

public class ChangeExpiredPasswordStoryTests : IDisposable
{
    private const string Username = "jdoe@test.local";
    private const string DistinguishedName = "CN=jdoe,DC=test,DC=local";
    private const string Audience = "portal-api-key";
    private const string OldPasswordExpirationDate = "10/17/2026 12:00:00";
    private readonly DefaultHttpContext _context = new();
    private readonly Mock<IApplicationCache> _cache = new();
    private readonly Mock<IMultiFactorApi> _api = new();
    private readonly Mock<IMultifactorIdpApi> _idp = new();
    private readonly Mock<IPasswordAttributeChanger> _attributeChanger = new();
    private readonly PortalSettings _settings = new()
    {
        PasswordManagement = new PasswordManagementSettings
        {
            Enabled = true,
            ChangeExpiredPasswordMode = ChangePasswordMode.AsUser
        }
    };
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(new string('k', 64))) { KeyId = "test-key" };
    private readonly ChangeExpiredPasswordStory _story;
    private readonly TokenVerifier _verifier;
    private string _oldToken = string.Empty;
    private string _newToken = string.Empty;

    public ChangeExpiredPasswordStoryTests()
    {
        var accessor = new SafeHttpContextAccessor(new HttpContextAccessor { HttpContext = _context });
        _context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(Constants.MultiFactorClaims.RawUserName, Username)], "test"));
        var protection = new DataProtection(new EphemeralDataProtectionProvider());
        _cache.Setup(x => x.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(Username)))
            .Returns(new CachedItem<string>(Username));
        _cache.Setup(x => x.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username)))
            .Returns(new CachedItem<string>(protection.Protect("old-password", Constants.PWD_RENEWAL_PURPOSE)));

        var connection = new Mock<ILdapConnectionAdapter>();
        connection.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<LdapIdentity>(), "old-password",
                It.IsAny<IMemoryCache>(), It.IsAny<Action<LdapConnectionAdapterConfigBuilder>>()))
            .ReturnsAsync(connection.Object);
        connection.Setup(x => x.WhereAmIAsync()).ReturnsAsync(LdapDomain.Parse("DC=test,DC=local"));
        connection.Setup(x => x.SearchQueryAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<LdapSearchScope>(), It.IsAny<string[]>()))
            .ReturnsAsync(new List<LdapEntry>
            {
                new() { Dn = DistinguishedName, DirectoryAttributes = new SearchResultAttributeCollection() }
            });
        var filterProviderMock = new Mock<ILdapProfileFilterProvider>();
        filterProviderMock.Setup(x => x.GetProfileSearchFilter(It.IsAny<LdapIdentity>()))
            .Returns(MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.LdapFilter.Create("objectClass", "user"));
        var filterProvider = filterProviderMock.Object;
        var connectionFactory = new LdapConnectionAdapterFactory(_settings,
            NullLogger<LdapConnectionAdapterFactory>.Instance, Mock.Of<IBindIdentityFormatter>(),
            connection.Object, filterProvider, _memoryCache);
        var profileLoader = new LdapProfileLoader(filterProvider, NullLogger<LdapProfileLoader>.Instance,
            new AdditionalClaimsMetadata(new AdditionalClaimDescriptorsProvider(_settings)), _settings);
        var localizer = new Mock<IStringLocalizer<SharedResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var changer = new UserPasswordChanger(connectionFactory, NullLogger<UserPasswordChanger>.Instance,
            _attributeChanger.Object, localizer.Object, profileLoader,
            new PasswordRequirementsService(_settings, Array.Empty<PasswordRequirementRule>()));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Constants.TOKEN_VALIDATION] = "{\"keys\":[{\"kty\":\"oct\",\"kid\":\"test-key\",\"k\":\""
                + Base64UrlEncoder.Encode(_key.Key) + "\"}]}",
            ["PortalSettings:MultiFactorApiSettings:ApiKey"] = Audience
        }).Build();
        _verifier = new TokenVerifier(config, _settings, NullLogger<TokenVerifier>.Instance);
        var authenticate = new AuthenticateSessionStory(_idp.Object, accessor, NullLogger<AuthenticateSessionStory>.Instance,
            _settings, _api.Object, _verifier);
        _story = new ChangeExpiredPasswordStory(_settings, accessor, protection, changer, _cache.Object,
            _api.Object, new HttpClientTokenProvider(accessor), _verifier,
            NullLogger<ChangeExpiredPasswordStory>.Instance, authenticate);
    }

    private void SetupTokens(string? saml = null, string? oidc = null, string? passwordExpirationDate = null)
    {
        _oldToken = CreateToken(true, saml, oidc, passwordExpirationDate);
        _newToken = CreateToken(false, saml, oidc);
        _context.Request.Headers.Cookie = $"{Constants.COOKIE_NAME}={_oldToken}";
        _api.Setup(x => x.RefreshAccessTokenAsync(_oldToken, It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ReturnsAsync(_newToken);
        _idp.Setup(x => x.LoginCompletedAsync(It.Is<LoginCompletedRequestDto>(r => r.AccessToken == _newToken),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new LoginCompletedResponseDto
            {
                Success = true,
                Action = saml != null ? LoginCompletedAction.BypassSaml
                    : oidc != null ? LoginCompletedAction.BypassOidc : LoginCompletedAction.Authenticated,
                SamlSessionId = saml!, OidcSessionId = oidc!, Identity = Username,
                TokenExpirationDate = DateTime.UtcNow.AddMinutes(5)
            });
    }

    private string CreateToken(bool expiredPassword, string? saml, string? oidc, string? passwordExpirationDate = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Username),
            new(JwtRegisteredClaimNames.Jti, "request-id"),
            new(Constants.MultiFactorClaims.RawUserName, Username),
            new("amr", "pwd"),
            new("department", "engineering"),
            new("groups", "group-a"),
            new("groups", "group-b")
        };
        if (expiredPassword) claims.Add(new Claim(Constants.MultiFactorClaims.ChangePassword, "true"));
        if (saml != null) claims.Add(new Claim(Constants.MultiFactorClaims.SamlSessionId, saml));
        if (oidc != null) claims.Add(new Claim(Constants.MultiFactorClaims.OidcSessionId, oidc));
        if (passwordExpirationDate != null) claims.Add(new Claim(Constants.MultiFactorClaims.PasswordExpirationDate, passwordExpirationDate));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("test", Audience, claims,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)));
    }

    [Theory]
    [InlineData("saml-session", null, "ByPassSamlSession", "samlSession", null)]
    [InlineData(null, "oidc-session", "ByPassOidcSession", "oidcSession", null)]
    [InlineData("saml-session", null, "ByPassSamlSession", "samlSession", OldPasswordExpirationDate)]
    [InlineData(null, "oidc-session", "ByPassOidcSession", "oidcSession", OldPasswordExpirationDate)]
    public async Task SsoPasswordChange_CompletesIdpWithRefreshedTokenBeforeContinuing(
        string? saml, string? oidc, string action, string sessionKey, string? passwordExpirationDate)
    {
        SetupTokens(saml, oidc, passwordExpirationDate);
        var stages = new List<string>();
        _attributeChanger.Setup(x => x.ExecuteChangeCommandAsync(DistinguishedName, "old-password", "new-password",
            It.IsAny<ILdapConnectionAdapter>())).Callback(() => stages.Add("ldap")).Returns(Task.CompletedTask);
        _api.Setup(x => x.RefreshAccessTokenAsync(_oldToken, It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Callback(() => stages.Add("refresh")).ReturnsAsync(_newToken);
        _idp.Setup(x => x.LoginCompletedAsync(It.Is<LoginCompletedRequestDto>(r => r.AccessToken == _newToken),
                It.IsAny<Dictionary<string, string>>()))
            .Callback(() => stages.Add("completed"))
            .ReturnsAsync(new LoginCompletedResponseDto
            {
                Success = true, Action = saml != null ? LoginCompletedAction.BypassSaml : LoginCompletedAction.BypassOidc,
                SamlSessionId = saml!, OidcSessionId = oidc!, Identity = Username,
                TokenExpirationDate = DateTime.UtcNow.AddMinutes(5)
            });

        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));

        Assert.Equal(new[] { "ldap", "refresh", "completed" }, stages);
        Assert.Equal(action, result.ActionName);
        Assert.Equal(saml ?? oidc, result.RouteValues![sessionKey]);
        Assert.Contains(_newToken, _context.Response.Headers.SetCookie.ToString());
        _cache.Verify(x => x.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username)), Times.Once);
        _api.Verify(x => x.RefreshAccessTokenAsync(_oldToken, It.Is<IReadOnlyDictionary<string, string>>(c =>
            c[Constants.MultiFactorClaims.RawUserName] == Username
            && !c.ContainsKey(Constants.MultiFactorClaims.ChangePassword)
            && !c.ContainsKey(Constants.MultiFactorClaims.PasswordExpirationDate)
            && c[saml != null ? Constants.MultiFactorClaims.SamlSessionId : Constants.MultiFactorClaims.OidcSessionId] == (saml ?? oidc))), Times.Once);
        _attributeChanger.Verify(x => x.ExecuteChangeCommandAsync(DistinguishedName, "old-password", "new-password",
            It.IsAny<ILdapConnectionAdapter>()), Times.Once);
    }

    [Fact]
    public void PasswordChangeClaims_PreserveCustomValuesAndRemoveOnlyReissuedClaims()
    {
        SetupTokens("saml-session", passwordExpirationDate: OldPasswordExpirationDate);
        var claims = _verifier.GetPasswordChangeClaims(_oldToken);

        Assert.Equal("pwd", claims["amr"]);
        Assert.Equal("engineering", claims["department"]);
        Assert.Equal(OldPasswordExpirationDate, claims[Constants.MultiFactorClaims.PasswordExpirationDate]);
        Assert.Equal(new[] { "group-a", "group-b" }, System.Text.Json.JsonSerializer.Deserialize<string[]>(claims["groups"]));
        Assert.Equal("true", claims[Constants.MultiFactorClaims.AdditionSsoStep]);
        foreach (var excluded in new[] { "iss", "aud", "sub", "jti", "exp", "iat", "nbf", Constants.MultiFactorClaims.ChangePassword })
        {
            Assert.False(claims.ContainsKey(excluded));
        }
    }

    [Fact]
    public void PasswordChangeClaims_InvalidSignatureIsRejected()
    {
        SetupTokens("saml-session");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(_oldToken);
        var invalidToken = new JwtSecurityToken(new JwtHeader(new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('z', 64))) { KeyId = "test-key" },
            SecurityAlgorithms.HmacSha256)), token.Payload);

        Assert.Throws<UnauthorizedException>(() => _verifier.GetPasswordChangeClaims(new JwtSecurityTokenHandler().WriteToken(invalidToken)));
    }

    [Fact]
    public void PasswordChangeClaims_PreserveTokenBinding()
    {
        SetupTokens("saml-session");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(_oldToken);
        token.Payload["cnf"] = new Dictionary<string, string> { ["jkt"] = "key-thumbprint" };
        var boundToken = new JwtSecurityToken(new JwtHeader(new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)), token.Payload);

        var claims = _verifier.GetPasswordChangeClaims(new JwtSecurityTokenHandler().WriteToken(boundToken));

        Assert.Equal("key-thumbprint", claims["cnf_jkt"]);
        Assert.False(claims.ContainsKey("cnf"));
    }

    [Fact]
    public async Task PasswordChangeWithoutSso_PreservesDonePage()
    {
        SetupTokens();
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));
        Assert.Equal("Done", result.ActionName);
        Assert.Equal("ExpiredPassword", result.ControllerName);
        _api.Verify(x => x.RefreshAccessTokenAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
        _idp.Verify(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task LdapFailure_KeepsPasswordChangeSessionAndDoesNotRefresh()
    {
        SetupTokens("saml-session");
        _attributeChanger.Setup(x => x.ExecuteChangeCommandAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<ILdapConnectionAdapter>())).ThrowsAsync(new Exception("ldap failed"));
        await Assert.ThrowsAsync<ModelStateErrorException>(() => _story.ExecuteAsync(new() { NewPassword = "new-password" }));
        _cache.Verify(x => x.Remove(It.IsAny<string>()), Times.Never);
        _api.Verify(x => x.RefreshAccessTokenAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task RefreshFailure_ClearsSessionAndPreservesSsoForLogin()
    {
        SetupTokens("saml-session");
        _api.Setup(x => x.RefreshAccessTokenAsync(_oldToken, It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ThrowsAsync(new Exception("api unavailable"));
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("saml-session", result.RouteValues!["samlSessionId"]);
        _cache.Verify(x => x.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(Username)), Times.Once);
        _idp.Verify(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        Assert.DoesNotContain(_newToken, _context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task RefreshedTokenWithDifferentSession_DoesNotCompleteIdp()
    {
        SetupTokens("original-session");
        _newToken = CreateToken(false, "different-session", null);
        _api.Setup(x => x.RefreshAccessTokenAsync(_oldToken, It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ReturnsAsync(_newToken);
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("original-session", result.RouteValues!["samlSessionId"]);
        _idp.Verify(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        Assert.DoesNotContain(_newToken, _context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task IdpTransportFailure_RestartsLoginWithSameSsoSession()
    {
        SetupTokens(null, "oidc-session");
        _idp.Setup(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new Exception("idp unavailable"));
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));
        Assert.Equal("Login", result.ActionName);
        Assert.Equal("oidc-session", result.RouteValues!["oidcSessionId"]);
        Assert.DoesNotContain(_newToken, _context.Response.Headers.SetCookie.ToString());
    }

    [Theory]
    [InlineData("saml-session", null)]
    [InlineData(null, "oidc-session")]
    public async Task IdpErrorResult_RestartsLoginWithOriginalSsoContext(string? saml, string? oidc)
    {
        SetupTokens(saml, oidc);
        _idp.Setup(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(LoginCompletedResponseDto.Failed("idp unavailable"));

        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));

        Assert.Equal("Login", result.ActionName);
        Assert.Equal(saml, result.RouteValues!["samlSessionId"]);
        Assert.Equal(oidc, result.RouteValues["oidcSessionId"]);
        Assert.DoesNotContain(_newToken, _context.Response.Headers.SetCookie.ToString());
        _attributeChanger.Verify(x => x.ExecuteChangeCommandAsync(DistinguishedName, "old-password", "new-password",
            It.IsAny<ILdapConnectionAdapter>()), Times.Once);
        _cache.Verify(x => x.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username)), Times.Once);
    }

    [Fact]
    public async Task IdpDenial_DoesNotContinueSsoOrSetRefreshedCookie()
    {
        SetupTokens("saml-session");
        _idp.Setup(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new LoginCompletedResponseDto { Success = false, Action = LoginCompletedAction.AccessDenied });
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));
        Assert.Equal("AccessDenied", result.ActionName);
        Assert.DoesNotContain(_newToken, _context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task PasswordManagementDisabled_DoesNotChangePassword()
    {
        _settings.PasswordManagement = new PasswordManagementSettings { Enabled = false };
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(new() { NewPassword = "new-password" }));
        Assert.Equal("Login", result.ActionName);
        _attributeChanger.Verify(x => x.ExecuteChangeCommandAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<ILdapConnectionAdapter>()), Times.Never);
    }

    public void Dispose() => _memoryCache.Dispose();
}
