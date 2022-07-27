namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    /// <summary>
    /// Generic Api response
    /// </summary>
    public record ApiResponse(bool Success, string Message)
    {
        public override string ToString()
        {
            return $"success: {Success}, message: {Message}";
        }
    }

    /// <summary>
    /// Api response with data
    /// </summary>
    public record ApiResponse<TModel>(TModel Model, bool Success, string Message) : ApiResponse(Success, Message);
}
