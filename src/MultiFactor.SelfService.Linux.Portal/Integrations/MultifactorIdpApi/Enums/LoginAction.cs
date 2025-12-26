namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;

public enum LoginAction
{
    Error,
    Authenticated,
    MfaRequired,
    BypassSaml,
    BypassOidc,
    ChangePassword,
    AccessDenied
}