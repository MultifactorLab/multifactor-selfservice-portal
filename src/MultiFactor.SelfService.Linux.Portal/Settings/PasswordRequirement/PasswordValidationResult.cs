namespace MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement;

public class PasswordValidationResult(string errorMessage = null)
{
    public bool IsValid => string.IsNullOrEmpty(ErrorMessage);
    private string ErrorMessage { get; } = errorMessage;

    public override string ToString()
    {
        return ErrorMessage;
    }
}