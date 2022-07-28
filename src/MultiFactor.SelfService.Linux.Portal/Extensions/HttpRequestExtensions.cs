namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool IsAjaxCall(this HttpRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            return request.Headers["x-requested-with"] == "XMLHttpRequest";
        }
    }
}
