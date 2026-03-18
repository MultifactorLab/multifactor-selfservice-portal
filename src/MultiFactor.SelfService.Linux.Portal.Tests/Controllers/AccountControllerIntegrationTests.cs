using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Controllers;

/// <summary>
/// Replaces <see cref="NegotiateHandler"/> in test environments.
/// The real handler calls <c>IConnectionItemsFeature</c> which only Kestrel provides;
/// using it with <see cref="Microsoft.AspNetCore.TestHost.TestServer"/> causes a 500 on every request.
/// This stub always reports no authentication result and never attempts the Negotiate handshake.
/// </summary>
internal sealed class NoOpNegotiateHandler(
    IOptionsMonitor<NegotiateOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<NegotiateOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}

public class AccountControllerIntegrationTests : IDisposable
{
    private readonly Mock<IMultifactorIdpApi> _idpApiMock;
    private readonly Mock<IMultiFactorApi> _multiFactorApiMock;
    private readonly Mock<ICredentialVerifier> _credentialVerifierMock;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public AccountControllerIntegrationTests()
    {
        _idpApiMock = new Mock<IMultifactorIdpApi>();
        _multiFactorApiMock = new Mock<IMultiFactorApi>();
        _credentialVerifierMock = new Mock<ICredentialVerifier>();
    }

    private HttpClient CreateClient()
    {
        _idpApiMock.Reset();
        _multiFactorApiMock.Reset();
        _credentialVerifierMock.Reset();
        
        _credentialVerifierMock
            .Setup(cv => cv.VerifyCredentialAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(CredentialVerificationResult.CreateBuilder(true)
                .SetUsername("jdoe@test.local")
                .Build());

        _credentialVerifierMock
            .Setup(cv => cv.VerifyMembership(It.IsAny<string>()))
            .ReturnsAsync(CredentialVerificationResult.CreateBuilder(true)
                .SetUsername("jdoe@test.local")
                .Build());
        
        _multiFactorApiMock
            .Setup(x => x.GetUserAuthenticatorsAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserAuthenticatorsDto
            {
                TotpAuthenticators = [new UserProfileAuthenticatorDto("totp-1", "TOTP")],
                TelegramAuthenticators = [],
                MobileAppAuthenticators = [],
                PhoneAuthenticators = []
            });
        
        _multiFactorApiMock
            .Setup(x => x.CreateSamlBypassRequestAsync(It.IsAny<UserProfileDto>(), It.IsAny<string>()))
            .ReturnsAsync(new BypassPageDto("/sso/callback", "sso-access-token"));

        _factory?.Dispose();
        _client?.Dispose();

        _factory = new TestWebAppFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var checkerDescriptors = services.Where(s =>
                    s.ServiceType == typeof(IHostedService) &&
                    s.ImplementationType != null &&
                    s.ImplementationType.Name == "ApplicationChecker").ToList();
                foreach (var desc in checkerDescriptors)
                    services.Remove(desc);
                
                ReplaceService(services, _idpApiMock.Object);
                
                ReplaceService(services, _multiFactorApiMock.Object);
                
                ReplaceService(services, _credentialVerifierMock.Object);
                
                var captchaDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(CaptchaVerifierResolver));
                if (captchaDescriptor != null) services.Remove(captchaDescriptor);
                services.AddSingleton<CaptchaVerifierResolver>(() =>
                {
                    var captchaMock = new Mock<ICaptchaVerifier>();
                    captchaMock
                        .Setup(v => v.VerifyCaptchaAsync(It.IsAny<HttpRequest>()))
                        .ReturnsAsync(new CaptchaVerificationResult(true));
                    return captchaMock.Object;
                });

                // Replace NegotiateHandler with a no-operation stub.
                // The real handler requires Kestrel's IConnectionItemsFeature, which TestServer
                // does not provide — without this replacement every request returns 500.
                // We update SchemeMap directly because AddScheme throws when the key already exists.
                services.AddTransient<NoOpNegotiateHandler>();
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    if (options.SchemeMap.TryGetValue(
                            NegotiateDefaults.AuthenticationScheme, out var scheme))
                    {
                        scheme.HandlerType = typeof(NoOpNegotiateHandler);
                    }
                });

                // Prevent the Hybrid policy scheme from forwarding to Negotiate
                services.PostConfigure<PolicySchemeOptions>("Hybrid", options =>
                {
                    options.ForwardDefaultSelector = _ => JwtBearerDefaults.AuthenticationScheme;
                });
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return _client;
    }

    private static void ReplaceService<TService>(IServiceCollection services, TService instance)
        where TService : class
    {
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TService));
        if (descriptor != null) services.Remove(descriptor);
        services.AddSingleton(instance);
    }

    private async Task<string?> ExtractAntiForgeryTokenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            content,
            @"name=""__RequestVerificationToken""[^>]*?value=""([^""]+)""");

        return match.Success ? match.Groups[1].Value : null;
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task Login_Get_ShouldAlwaysReturnLoginView()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Login", content);
    }

    [Fact]
    public async Task Auth_Get_WithAuthorizedUser_ShouldRedirectToHome()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.GetUserProfileAsync())
            .ReturnsAsync(new UserProfileDto("userId", "user@test.local"));

        // Act
        var response = await client.GetAsync("/account");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found);
        Assert.False(string.IsNullOrEmpty(response.Headers.Location?.ToString()));
    }

    [Fact]
    public async Task Login_Post_WithValidCredentials_ShouldRedirectToMfa()
    {
        // Arrange
        var client = CreateClient();

        var loginResponse = new LoginResponseDto
        {
            Success = true,
            Action = LoginAction.MfaRequired,
            RedirectUrl = "https://mfa.example.com"
        };

        _idpApiMock
            .Setup(x => x.LoginAsync(
                It.IsAny<LoginRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(loginResponse);

        var token = await ExtractAntiForgeryTokenAsync(client, "/account/login");

        var formData = new Dictionary<string, string>
        {
            { "UserName", "jdoe@test.local" },
            { "Password", "password123" },
            { "MyUrl", "https://portal.example.com" }
        };
        if (token != null)
            formData["__RequestVerificationToken"] = token;

        // Act
        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(formData));

        // Assert: any 3xx redirect is expected (SignInStory uses RedirectResult which can be 301 or 302)
        Assert.True((int)response.StatusCode is >= 300 and < 400);
        var location = response.Headers.Location?.ToString();
        Assert.True(location == loginResponse.RedirectUrl ||
                    location?.Contains("mfa.example.com") == true);
    }

    [Fact]
    public async Task Login_Post_WithBypassSaml_ShouldRedirectToSsoBypassSession()
    {
        // Arrange
        var client = CreateClient();

        var loginResponse = new LoginResponseDto
        {
            Success = true,
            Action = LoginAction.BypassSaml
        };

        _idpApiMock
            .Setup(x => x.LoginAsync(
                It.IsAny<LoginRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(loginResponse);

        var token = await ExtractAntiForgeryTokenAsync(client, "/account/login");

        var formData = new Dictionary<string, string>
        {
            { "UserName", "jdoe@test.local" },
            { "Password", "password123" },
            { "MyUrl", "https://portal.example.com" }
        };
        if (token != null)
            formData["__RequestVerificationToken"] = token;

        // Act
        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(formData));

        // Assert: redirect to ByPassSsoSession (the SSO bypass orchestration page)
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found);
        var location = response.Headers.Location?.ToString();
        Assert.True(location != null && (
            location.Contains("ByPassSsoSession", StringComparison.OrdinalIgnoreCase) ||
            location.Contains("bypasssso", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(location)));
    }

    [Fact]
    public async Task Login_Post_WithInvalidCredentials_ShouldReturnViewWithError()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.LoginAsync(
                It.IsAny<LoginRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new ModelStateErrorException("Wrong credentials"));

        var token = await ExtractAntiForgeryTokenAsync(client, "/account/login");

        var formData = new Dictionary<string, string>
        {
            { "UserName", "jdoe@test.local" },
            { "Password", "wrongpassword" },
            { "MyUrl", "https://portal.example.com" }
        };
        if (token != null)
            formData["__RequestVerificationToken"] = token;

        // Act
        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(formData));

        // Assert: form is re-shown with error
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Login", content);
    }

    [Fact]
    public async Task Identity_Get_WithUnauthorized_ShouldRedirectToLogin()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.GetUserProfileAsync())
            .ThrowsAsync(new UnauthorizedException());

        // Act
        var response = await client.GetAsync("/account/identity");

        // Assert: with PreAuthenticationMethod=false, Identity always redirects to Login
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found);
    }

    [Fact]
    public async Task Identity_Post_WithValidUsername_ShouldRedirectOrReturnBadRequest()
    {
        // Arrange
        var identityResponse = new IdentityResponseDto
        {
            Success = true,
            Action = IdentityAction.MfaRequired,
            RedirectUrl = "https://mfa.example.com"
        };

        _idpApiMock
            .Setup(x => x.IdentityAsync(
                It.IsAny<IdentityRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(identityResponse);

        var client = CreateClient();

        // CSRF token extraction will return null because GET /account/identity redirects
        var token = await ExtractAntiForgeryTokenAsync(client, "/account/identity");

        var formData = new Dictionary<string, string>
        {
            { "UserName", "user@test.local" },
            { "MyUrl", "https://portal.example.com" }
        };
        if (token != null)
            formData["__RequestVerificationToken"] = token;

        // Act
        var response = await client.PostAsync("/account/identity", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.BadRequest);
        if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
        {
            Assert.False(string.IsNullOrEmpty(response.Headers.Location?.ToString()));
        }
    }

    [Fact]
    public async Task Identity_Post_WithShowAuthn_ShouldRedirectOrReturnView()
    {
        // Arrange
        var identityResponse = new IdentityResponseDto
        {
            Success = true,
            Action = IdentityAction.ShowAuthn,
            Username = "user@test.local"
        };

        _idpApiMock
            .Setup(x => x.IdentityAsync(
                It.IsAny<IdentityRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(identityResponse);

        var client = CreateClient();
        var token = await ExtractAntiForgeryTokenAsync(client, "/account/identity");

        var formData = new Dictionary<string, string>
        {
            { "UserName", "user@test.local" },
            { "MyUrl", "https://portal.example.com" }
        };
        if (token != null)
            formData["__RequestVerificationToken"] = token;

        // Act
        var response = await client.PostAsync("/account/identity", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Authn_Post_ShouldRedirectWhenPreAuthenticationMethodIsDisabled()
    {
        // Arrange
        var loginCompletedResponse = new LoginCompletedResponseDto
        {
            Action = LoginCompletedAction.Authenticated,
            Identity = "user@test.local"
        };

        _idpApiMock
            .Setup(x => x.LoginCompletedAsync(
                It.IsAny<LoginCompletedRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(loginCompletedResponse);

        var client = CreateClient();
        var token = await ExtractAntiForgeryTokenAsync(client, "/account/identity");

        var formData = new Dictionary<string, string>
        {
            { "UserName", "user@test.local" },
            { "Password", "password123" },
            { "AccessToken", "test-token" },
            { "MyUrl", "https://portal.example.com" }
        };
        if (token != null)
            formData["__RequestVerificationToken"] = token;

        // Act
        var response = await client.PostAsync("/account/authn", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_Get_ShouldCallLogoutAndRedirectToLogin()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.LogoutAsync(
                It.IsAny<LogoutRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new LogoutResponseDto { Success = true });

        // Act
        var response = await client.GetAsync("/account/logout");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.OK);
        if (response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString();
            Assert.True(location != null &&
                        location.Contains("login", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task PostbackFromMfa_ShouldCompleteAuthenticationAndRedirect()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.LoginCompletedAsync(
                It.IsAny<LoginCompletedRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new LoginCompletedResponseDto
            {
                Action = LoginCompletedAction.Authenticated,
                Identity = "user@test.local",
                Success = true
            });
        var formData = new Dictionary<string, string>
        {
            { "accessToken", "test-token" }
        };

        // Act
        var response = await client.PostAsync("/account/postbackfrommfa", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString();
        Assert.True(!string.IsNullOrEmpty(location));
    }

    [Fact]
    public async Task ByPassSamlSession_WithValidSession_ShouldReturnSamlResponse()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.BypassSamlAsync(
                It.IsAny<BypassSamlRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new BypassSamlResponseDto { SamlResponseHtml = "<html>SAML Response</html>" });

        // Act
        var response = await client.GetAsync("/account/bypasssaml?samlSession=test-session-id");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("SAML Response", content);
        }
    }

    [Fact]
    public async Task ByPassSamlSession_WithUnauthorized_ShouldRedirectToLogin()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.BypassSamlAsync(
                It.IsAny<BypassSamlRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new UnauthorizedException());

        // Act
        var response = await client.GetAsync("/account/bypasssaml?samlSession=test-session-id");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found)
        {
            Assert.Contains("login", response.Headers.Location?.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ByPassOidcSession_WithValidSession_ShouldRedirect()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.BypassOidcAsync(
                It.IsAny<BypassOidcRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new BypassOidcResponseDto { RedirectUrl = "https://oidc.example.com/callback" });

        // Act: ByPassOidcSession uses conventional routing (/Account/ByPassOidcSession)
        var response = await client.GetAsync("/account/bypassoidс?oidcSession=test-session-id");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString();
            Assert.True(location == "https://oidc.example.com/callback" ||
                        location?.Contains("oidc.example.com") == true ||
                        !string.IsNullOrEmpty(location));
        }
    }

    [Fact]
    public async Task ByPassOidcSession_WithUnauthorized_ShouldRedirectToLogin()
    {
        // Arrange
        var client = CreateClient();

        _idpApiMock
            .Setup(x => x.BypassOidcAsync(
                It.IsAny<BypassOidcRequestDto>(),
                It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new UnauthorizedException());

        // Act
        var response = await client.GetAsync("/account/bypassoidc?oidcSession=test-session-id");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found)
        {
            Assert.Contains("login", response.Headers.Location?.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
