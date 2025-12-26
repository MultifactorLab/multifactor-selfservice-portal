namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;

public enum LoginCompletedAction
{
    Error,
    Authenticated,
    BypassSaml,
    BypassOidc,
    ChangePassword
}