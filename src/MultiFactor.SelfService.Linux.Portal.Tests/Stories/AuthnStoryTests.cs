using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Linux.Portal.Stories.SignIn;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Stories;

public class AuthnStoryTests
{
    private readonly Mock<ICredentialVerifier> _verifier = new();
    private readonly Mock<IMultifactorIdpApi> _idp = new();
    private readonly Mock<IApplicationCache> _cache = new();
    private readonly PortalSettings _settings = TestEnvironment.LoadPortalSettings(
        TestEnvironment.GetAssetPath(Path.Combine("Settings", "minimal-settings.xml")));
    private readonly AuthnStory _story;
    private readonly PasswordChangeTokens _tokens = new();
    private readonly Mock<IMultiFactorApi> _api = new();

    public AuthnStoryTests()
    {
        _settings.PasswordManagement = new PasswordManagementSettings { Enabled = true };
        var accessor = new SafeHttpContextAccessor(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        var authenticate = new AuthenticateSessionStory(_idp.Object, accessor, NullLogger<AuthenticateSessionStory>.Instance,
            _settings, _api.Object, _tokens.CreateVerifier(_settings));
        _story = new AuthnStory(_verifier.Object, new DataProtection(new EphemeralDataProtectionProvider()),
            accessor, _settings, _cache.Object, Mock.Of<IStringLocalizer<SharedResource>>(),
            NullLogger<SignInStory>.Instance, authenticate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task WithoutMfaToken_ShowsLoginWithoutAuthenticating(string? token)
    {
        var result = Assert.IsType<ViewResult>(await _story.ExecuteAsync(new IdentityViewModel
        {
            UserName = "jdoe@test.local", Password = "password", AccessToken = token!
        }));
        Assert.Equal("Login", result.ViewName);
        var model = Assert.IsType<LoginViewModel>(result.Model);
        Assert.Equal("jdoe@test.local", model.UserName);
        Assert.Null(model.Password);
        _verifier.Verify(x => x.VerifyCredentialAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _idp.Verify(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
    }

    [Theory]
    [InlineData(LoginCompletedAction.Authenticated, true, "Index")]
    [InlineData(LoginCompletedAction.BypassSaml, true, "ByPassSamlSession")]
    [InlineData(LoginCompletedAction.BypassOidc, true, "ByPassOidcSession")]
    [InlineData(LoginCompletedAction.Error, false, "AccessDenied")]
    public async Task ValidPassword_UsesIdpResult(LoginCompletedAction action, bool success, string expectedAction)
    {
        _verifier.Setup(x => x.VerifyCredentialAsync("jdoe@test.local", "password"))
            .ReturnsAsync(CredentialVerificationResult.CreateBuilder(true).SetUsername("jdoe@test.local").Build());
        SetupIdp(action, success);
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(Model()));
        Assert.Equal(expectedAction, result.ActionName);
        _api.Verify(x => x.RefreshAccessTokenAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
    }

    [Theory]
    [InlineData(true, "Change")]
    [InlineData(false, "ByPassSamlSession")]
    public async Task ExpiredPassword_RespectsPasswordManagementSetting(bool enabled, string expectedAction)
    {
        _settings.PasswordManagement = new PasswordManagementSettings { Enabled = enabled };
        _verifier.Setup(x => x.VerifyCredentialAsync("jdoe@test.local", "password"))
            .ReturnsAsync(CredentialVerificationResult.CreateBuilder(true).SetUsername("jdoe@test.local")
                .SetUserMustChangePassword(true).Build());
        SetupIdp(LoginCompletedAction.BypassSaml, true);
        var token = _tokens.Create(true);
        SetupIdp(LoginCompletedAction.ChangePassword, true, token);
        var model = Model();
        model.AccessToken = enabled ? token : "mfa-token";
        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecuteAsync(model));
        Assert.Equal(expectedAction, result.ActionName);
        _cache.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<string>()), enabled ? Times.Exactly(2) : Times.Never());
    }

    private static IdentityViewModel Model() => new()
    {
        UserName = "jdoe@test.local", Password = "password", AccessToken = "mfa-token"
    };

    private void SetupIdp(LoginCompletedAction action, bool success, string token = "mfa-token")
    {
        _idp.Setup(x => x.LoginCompletedAsync(It.Is<LoginCompletedRequestDto>(r => r.AccessToken == token),
            It.IsAny<Dictionary<string, string>>())).ReturnsAsync(new LoginCompletedResponseDto
        {
            Success = success, Action = action, SamlSessionId = "saml-session", OidcSessionId = "oidc-session"
        });
    }
}
