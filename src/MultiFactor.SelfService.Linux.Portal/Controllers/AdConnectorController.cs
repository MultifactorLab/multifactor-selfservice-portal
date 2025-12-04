using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Controllers;

/// <summary>
/// AD Connector API for IdP to verify credentials and membership.
/// This controller proxies AD operations from IdP to the local AD.
/// </summary>
[ApiController]
[Route("api/v1/ad")]
[AllowAnonymous] // TODO: Add mTLS authentication in production
public class AdConnectorController : ControllerBase
{
    private readonly CredentialVerifier _credentialVerifier;
    private readonly PortalSettings _settings;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<AdConnectorController> _logger;

    public AdConnectorController(
        CredentialVerifier credentialVerifier,
        PortalSettings settings,
        IStringLocalizer<SharedResource> localizer,
        ILogger<AdConnectorController> logger)
    {
        _credentialVerifier = credentialVerifier;
        _settings = settings;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Verifies user credentials against AD.
    /// </summary>
    [HttpPost("verify-credentials")]
    public async Task<IActionResult> VerifyCredentials([FromBody] VerifyCredentialsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AdConnectorResponse
            {
                Success = false,
                Message = "Username and password are required"
            });
        }
        
        var serviceUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User!);
        var userName = LdapIdentity.ParseUser(request.Username);
        if (userName.IsEquivalentTo(serviceUser))
        {
            return BadRequest(new AdConnectorResponse
            {
                Success = false,
                Message = "Invalid credentials"
            });
        }

        try
        {
            var result = await _credentialVerifier.VerifyCredentialAsync(
                request.Username.Trim(),
                request.Password.Trim());

            var response = MapToResponse(result);
            return Ok(new AdConnectorResponse<VerifyCredentialsResponse>
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying credentials for user '{Username}'", request.Username);
            return StatusCode(500, new AdConnectorResponse
            {
                Success = false,
                Message = "Internal error during credential verification"
            });
        }
    }

    /// <summary>
    /// Verifies user membership and loads profile from AD (without password).
    /// Used in pre-authentication flow.
    /// </summary>
    [HttpPost("verify-membership")]
    public async Task<IActionResult> VerifyMembership([FromBody] VerifyMembershipRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new AdConnectorResponse
            {
                Success = false,
                Message = "Username is required"
            });
        }

        try
        {
            var result = await _credentialVerifier.VerifyMembership(request.Username.Trim());

            var response = MapToResponse(result);
            return Ok(new AdConnectorResponse<VerifyCredentialsResponse>
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying membership for user '{Username}'", request.Username);
            return StatusCode(500, new AdConnectorResponse
            {
                Success = false,
                Message = "Internal error during membership verification"
            });
        }
    }

    private static VerifyCredentialsResponse MapToResponse(CredentialVerificationResult result)
    {
        return new VerifyCredentialsResponse
        {
            IsAuthenticated = result.IsAuthenticated,
            IsBypass = result.IsBypass,
            UserMustChangePassword = result.UserMustChangePassword,
            PasswordExpirationDate = result.PasswordExpirationDate,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Phone = result.Phone,
            Username = result.Username,
            UserPrincipalName = result.UserPrincipalName,
            CustomIdentity = result.CustomIdentity,
            Reason = result.Reason
        };
    }
}

#region DTOs

/// <summary>
/// Request DTO for credential verification.
/// </summary>
public sealed class VerifyCredentialsRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Request DTO for membership verification.
/// </summary>
public sealed class VerifyMembershipRequest
{
    public string Username { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for credential/membership verification.
/// </summary>
public sealed class VerifyCredentialsResponse
{
    public bool IsAuthenticated { get; init; }
    public bool IsBypass { get; init; }
    public bool UserMustChangePassword { get; init; }
    public DateTime PasswordExpirationDate { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Username { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? CustomIdentity { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Standard response wrapper for AD Connector API.
/// </summary>
public class AdConnectorResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Standard response wrapper with data for AD Connector API.
/// </summary>
public sealed class AdConnectorResponse<T> : AdConnectorResponse
{
    public T? Data { get; init; }
}

#endregion

