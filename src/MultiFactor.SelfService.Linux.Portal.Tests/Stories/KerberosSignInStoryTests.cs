using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.SignIn;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Stories;

public class KerberosSignInStoryTests
{
    private readonly Mock<IMultifactorIdpApi> _idpApiMock;
    private readonly Mock<ICredentialVerifier> _credentialVerifierMock;
    private readonly Mock<ILogger<KerberosSignInStory>> _loggerMock;

    // ClaimsProvider.GetClaims is non-virtual, so we use a real instance with no sources.
    private static readonly ClaimsProvider EmptyClaimsProvider =
        new(Array.Empty<IClaimsSource>());

    // minimal-settings.xml has TechnicalAccountSettings.User = "user"
    private static readonly PortalSettings Settings = TestEnvironment.LoadPortalSettings(
        TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml"));

    private static readonly Dictionary<string, string> EmptyHeaders = new();
    private static readonly SingleSignOnDto NoSso = new(string.Empty, string.Empty);
    private const string PostbackUrl = "https://portal.example.com/account/postbackfrommfa";

    public KerberosSignInStoryTests()
    {
        _idpApiMock = new Mock<IMultifactorIdpApi>();
        _credentialVerifierMock = new Mock<ICredentialVerifier>();
        _loggerMock = new Mock<ILogger<KerberosSignInStory>>();
    }

    private KerberosSignInStory CreateStory() => new(
        _idpApiMock.Object,
        Settings,
        _loggerMock.Object,
        EmptyClaimsProvider,
        _credentialVerifierMock.Object);

    private static ClaimsPrincipal MakePrincipal(string? name) =>
        new(new ClaimsIdentity(
            name != null
                ? [new System.Security.Claims.Claim(ClaimTypes.Name, name)]
                : [],
            "Negotiate"));

    private static CredentialVerificationResult SuccessfulMembership(string username = "jdoe@domain.local") =>
        CredentialVerificationResult.CreateBuilder(true)
            .SetUsername(username)
            .SetUserPrincipalName(username)
            .Build();

    private void SetupSuccessfulMembershipVerification(string username = "jdoe@domain.local") =>
        _credentialVerifierMock
            .Setup(x => x.VerifyMembership(It.IsAny<string>()))
            .ReturnsAsync(SuccessfulMembership(username));

    private void SetupIdpLoginResponse(LoginResponseDto response) =>
        _idpApiMock
            .Setup(x => x.LoginAsync(
                It.IsAny<LoginRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(response);

    // -------------------------------------------------------------------------
    // Username extraction
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_WhenPrincipalHasNoNameClaim_RedirectsToLogin()
    {
        var result = await CreateStory().ExecuteAsync(
            MakePrincipal(null), NoSso, EmptyHeaders, PostbackUrl);

        AssertRedirectToLogin(result);
        _credentialVerifierMock.Verify(
            x => x.VerifyMembership(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPrincipalHasEmptyName_RedirectsToLogin()
    {
        var result = await CreateStory().ExecuteAsync(
            MakePrincipal(string.Empty), NoSso, EmptyHeaders, PostbackUrl);

        AssertRedirectToLogin(result);
        _credentialVerifierMock.Verify(
            x => x.VerifyMembership(It.IsAny<string>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Technical-account guard
    // The technical account in minimal-settings.xml is User="user".
    // Both UPN and NetBIOS forms of that username must be blocked.
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("user@domain.local")] // UPN  → UID "user"
    [InlineData("DOMAIN\\user")]      // NetBIOS → UID "user"
    public async Task ExecuteAsync_WhenKerberosPrincipalMatchesTechnicalAccount_RedirectsToLogin(
        string kerberosName)
    {
        var result = await CreateStory().ExecuteAsync(
            MakePrincipal(kerberosName), NoSso, EmptyHeaders, PostbackUrl);

        AssertRedirectToLogin(result);
        _credentialVerifierMock.Verify(
            x => x.VerifyMembership(It.IsAny<string>()), Times.Never);
        _idpApiMock.Verify(
            x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // Membership verification
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_WhenMembershipVerificationFails_RedirectsToLoginWithoutCallingIdp()
    {
        _credentialVerifierMock
            .Setup(x => x.VerifyMembership(It.IsAny<string>()))
            .ReturnsAsync(CredentialVerificationResult.FromUnknownError("Not a member of required group"));

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        AssertRedirectToLogin(result);
        _idpApiMock.Verify(
            x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMembershipIsBypass_ProceedsToIdpLogin()
    {
        _credentialVerifierMock
            .Setup(x => x.VerifyMembership(It.IsAny<string>()))
            .ReturnsAsync(CredentialVerificationResult.ByPass("jdoe", "jdoe@domain.local", false));
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.MfaRequired, RedirectUrl = "https://mfa.example.com"
        });

        await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        _idpApiMock.Verify(
            x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserMustChangePassword_MembershipCountsAsValid()
    {
        _credentialVerifierMock
            .Setup(x => x.VerifyMembership(It.IsAny<string>()))
            .ReturnsAsync(CredentialVerificationResult.CreateBuilder(false)
                .SetUsername("jdoe@domain.local")
                .SetUserMustChangePassword(true)
                .Build());
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.MfaRequired, RedirectUrl = "https://mfa.example.com"
        });

        await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        _idpApiMock.Verify(
            x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // IDP request payload verification
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_SendsKerberosAmrClaimInIdpRequest()
    {
        SetupSuccessfulMembershipVerification();

        LoginRequestDto? captured = null;
        _idpApiMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<LoginRequestDto, Dictionary<string, string>>((req, _) => captured = req)
            .ReturnsAsync(new LoginResponseDto
            {
                Success = true, Action = LoginAction.MfaRequired, RedirectUrl = "https://mfa.example.com"
            });

        await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        Assert.NotNull(captured);
        Assert.True(captured!.AdditionalClaims.TryGetValue(
            Core.Constants.AuthenticationClaims.AUTHENTICATION_METHODS_REFERENCES, out var amr));
        Assert.Equal(Core.Constants.AuthenticationClaims.KERBEROS_METHOD, amr);
    }

    [Fact]
    public async Task ExecuteAsync_PassesSsoSessionIdsAndPostbackUrlToIdp()
    {
        var sso = new SingleSignOnDto("saml-123", "oidc-456");
        SetupSuccessfulMembershipVerification();

        LoginRequestDto? captured = null;
        _idpApiMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<LoginRequestDto, Dictionary<string, string>>((req, _) => captured = req)
            .ReturnsAsync(new LoginResponseDto
            {
                Success = true, Action = LoginAction.MfaRequired, RedirectUrl = "https://mfa.example.com"
            });

        await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), sso, EmptyHeaders, PostbackUrl);

        Assert.Equal("saml-123", captured!.SamlSessionId);
        Assert.Equal("oidc-456", captured.OidcSessionId);
        Assert.Equal(PostbackUrl, captured.LoginCompletedCallbackUrl);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsHttpHeadersToIdp()
    {
        var headers = new Dictionary<string, string> { ["X-Forwarded-For"] = "10.0.0.1" };
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.MfaRequired, RedirectUrl = "https://mfa.example.com"
        });

        await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, headers, PostbackUrl);

        _idpApiMock.Verify(x =>
            x.LoginAsync(
                It.IsAny<LoginRequestDto>(),
                It.Is<Dictionary<string, string>>(h => h["X-Forwarded-For"] == "10.0.0.1")),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // HandleLoginResponse — all IDP response outcomes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_WhenIdpReturnsFailure_RedirectsToLogin()
    {
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = false, ErrorMessage = "account locked"
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        AssertRedirectToLogin(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMfaRequired_RedirectsToMfaUrl()
    {
        const string mfaUrl = "https://mfa.example.com/auth";
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.MfaRequired, RedirectUrl = mfaUrl
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(mfaUrl, redirect.Url);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMfaRequired_ButRedirectUrlIsEmpty_FallsThroughToLogin()
    {
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.MfaRequired, RedirectUrl = string.Empty
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        // No MFA redirect URL → falls through to the catch-all redirect → Login
        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBypassSaml_RedirectsToByPassSamlSessionWithCorrectSession()
    {
        var sso = new SingleSignOnDto("saml-session-id", string.Empty);
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.BypassSaml
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), sso, EmptyHeaders, PostbackUrl);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ByPassSamlSession", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
        Assert.Equal("saml-session-id", redirect.RouteValues!["samlSession"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBypassOidc_RedirectsToByPassOidcSessionWithCorrectSession()
    {
        var sso = new SingleSignOnDto(string.Empty, "oidc-session-id");
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.BypassOidc
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), sso, EmptyHeaders, PostbackUrl);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ByPassOidcSession", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
        Assert.Equal("oidc-session-id", redirect.RouteValues!["oidcSession"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenChangePassword_RedirectsToLogin()
    {
        // Kerberos flow cannot handle password change inline; user is sent back to the login form.
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.ChangePassword
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        AssertRedirectToLogin(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccessDenied_RedirectsToErrorController()
    {
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.AccessDenied
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccessDenied", redirect.ActionName);
        Assert.Equal("Error", redirect.ControllerName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnexpectedActionWithFallbackUrl_RedirectsToFallbackUrl()
    {
        const string fallbackUrl = "https://portal.example.com/fallback";
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.Authenticated, RedirectUrl = fallbackUrl
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(fallbackUrl, redirect.Url);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnexpectedActionWithoutFallbackUrl_RedirectsToLogin()
    {
        SetupSuccessfulMembershipVerification();
        SetupIdpLoginResponse(new LoginResponseDto
        {
            Success = true, Action = LoginAction.Authenticated, RedirectUrl = string.Empty
        });

        var result = await CreateStory().ExecuteAsync(
            MakePrincipal("jdoe@domain.local"), NoSso, EmptyHeaders, PostbackUrl);

        AssertRedirectToLogin(result);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void AssertRedirectToLogin(IActionResult result)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Account", redirect.ControllerName);
    }
}
