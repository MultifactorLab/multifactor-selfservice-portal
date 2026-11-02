using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Stories;

public class AuthenticateSessionStoryTests
{
    private readonly AutoMocker _mocker;
    private readonly DefaultHttpContext _context;
    private readonly PortalSettings _settings;
    private readonly AuthenticateSessionStory _story;
    private readonly PasswordChangeTokens _tokens = new();

    public AuthenticateSessionStoryTests()
    {
        _mocker = new AutoMocker();
        _context = new DefaultHttpContext();
        _settings = new PortalSettings { PasswordManagement = new PasswordManagementSettings { Enabled = true } };
        _mocker.Use(_settings);
        _mocker.Use(_tokens.CreateVerifier(_settings));
        _mocker.Use(new SafeHttpContextAccessor(new HttpContextAccessor { HttpContext = _context }));
        _story = _mocker.CreateInstance<AuthenticateSessionStory>();
    }

    [Theory]
    [InlineData(LoginCompletedAction.Authenticated, "Index")]
    [InlineData(LoginCompletedAction.BypassSaml, "ByPassSamlSession")]
    [InlineData(LoginCompletedAction.BypassOidc, "ByPassOidcSession")]
    [InlineData(LoginCompletedAction.ChangePassword, "Change")]
    public async Task NormalCompletion_PreservesActionAndCookie(LoginCompletedAction action, string expectedAction)
    {
        _mocker.GetMock<IMultifactorIdpApi>().Setup(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(),
            It.IsAny<Dictionary<string, string>>())).ReturnsAsync(new LoginCompletedResponseDto
        {
            Success = true, Action = action, SamlSessionId = "saml-session", OidcSessionId = "oidc-session",
            TokenExpirationDate = DateTime.UtcNow.AddMinutes(5)
        });

        var result = Assert.IsType<RedirectToActionResult>(await _story.Execute("access-token"));

        Assert.Equal(expectedAction, result.ActionName);
        Assert.Contains("access-token", _context.Response.Headers.SetCookie.ToString());
        _mocker.GetMock<IMultiFactorApi>().Verify(x => x.RefreshAccessTokenAsync(
            It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Never);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task MandatoryChange_SendsSignedRequirementToIdpBeforeShowingForm(bool alreadyMarked, bool saml)
    {
        var original = _tokens.Create(alreadyMarked, saml ? "saml-session" : null, saml ? null : "oidc-session");
        var marked = _tokens.Create(true, saml ? "saml-session" : null, saml ? null : "oidc-session");
        var stages = new List<string>();
        _mocker.GetMock<IMultiFactorApi>().Setup(x => x.RefreshAccessTokenAsync(original,
            It.Is<IReadOnlyDictionary<string, string>>(c => c[Constants.MultiFactorClaims.ChangePassword] == "true"
                && c["department"] == "engineering" && c["amr"] == "pwd"
                && c["groups"] == "[\"group-a\",\"group-b\"]")))
            .Callback(() => stages.Add("refresh")).ReturnsAsync(marked);
        var expectedToken = alreadyMarked ? original : marked;
        _mocker.GetMock<IMultifactorIdpApi>().Setup(x => x.LoginCompletedAsync(
            It.Is<LoginCompletedRequestDto>(r => r.AccessToken == expectedToken), It.IsAny<Dictionary<string, string>>()))
            .Callback(() => stages.Add("completed")).ReturnsAsync(new LoginCompletedResponseDto
            {
                Success = true, Action = LoginCompletedAction.ChangePassword,
                TokenExpirationDate = DateTime.UtcNow.AddMinutes(5)
            });

        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecutePasswordChangeAsync(original));

        Assert.Equal("Change", result.ActionName);
        Assert.Equal(alreadyMarked ? new[] { "completed" } : new[] { "refresh", "completed" }, stages);
        Assert.Contains(expectedToken, _context.Response.Headers.SetCookie.ToString());
        _mocker.GetMock<IMultiFactorApi>().Verify(x => x.RefreshAccessTokenAsync(
            It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()), alreadyMarked ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData("missing-requirement")]
    [InlineData("identity")]
    [InlineData("request")]
    [InlineData("sso")]
    [InlineData("api-error")]
    [InlineData("signature")]
    public async Task MandatoryChange_RefreshFailureDoesNotCompleteLoginOrIssueToken(string failure)
    {
        var original = _tokens.Create(false);
        var refreshed = failure switch
        {
            "missing-requirement" => _tokens.Create(false),
            "identity" => _tokens.Create(true, identity: "another-user"),
            "request" => _tokens.Create(true, requestId: "another-request"),
            "sso" => _tokens.Create(true, saml: "another-session"),
            _ => _tokens.Create(true)
        };
        if (failure == "signature")
        {
            var start = refreshed.LastIndexOf('.') + 1;
            refreshed = refreshed[..start] + (refreshed[start] == 'A' ? "B" : "A") + refreshed[(start + 1)..];
        }
        var refresh = _mocker.GetMock<IMultiFactorApi>().Setup(x => x.RefreshAccessTokenAsync(original,
            It.IsAny<IReadOnlyDictionary<string, string>>()));
        if (failure == "api-error") refresh.ThrowsAsync(new InvalidOperationException("API unavailable"));
        else refresh.ReturnsAsync(refreshed);

        var result = Assert.IsType<RedirectToActionResult>(await _story.ExecutePasswordChangeAsync(original));

        Assert.Equal("Login", result.ActionName);
        Assert.Equal("saml-session", result.RouteValues!["samlSessionId"]);
        Assert.DoesNotContain(refreshed, _context.Response.Headers.SetCookie.ToString());
        _mocker.GetMock<IMultifactorIdpApi>().Verify(x => x.LoginCompletedAsync(
            It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task DisabledPasswordManagement_DeniesWithoutCreatingPortalSession()
    {
        _settings.PasswordManagement = new PasswordManagementSettings { Enabled = false };
        _mocker.GetMock<IMultifactorIdpApi>().Setup(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(),
            It.IsAny<Dictionary<string, string>>())).ReturnsAsync(new LoginCompletedResponseDto
        {
            Success = true, Action = LoginCompletedAction.ChangePassword,
            TokenExpirationDate = DateTime.UtcNow.AddMinutes(5)
        });

        var result = Assert.IsType<RedirectToActionResult>(await _story.Execute("access-token"));

        Assert.Equal("AccessDenied", result.ActionName);
        Assert.False(_context.Response.Headers.ContainsKey("Set-Cookie"));
    }
}
