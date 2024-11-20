namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool IsAjaxCall(this HttpRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            return request.Headers.XRequestedWith == "XMLHttpRequest";
        }
    }
}
