namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public class HttpClientTokenProvider
    {
        private readonly SafeHttpContextAccessor _contextAccessor;

        public HttpClientTokenProvider(SafeHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        public string GetToken() => _contextAccessor.HttpContext.Request.Cookies[Constants.COOKIE_NAME]
            ?? throw new Exception("HttpClient token not found");   
    }
}
