namespace MultiFactor.SelfService.Linux.Portal.Dto.AdConnector;

public class AdConnectorResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
}

public sealed class AdConnectorResponse<T> : AdConnectorResponse
{
    public T? Data { get; init; }
}