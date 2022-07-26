using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public class HttpClientTokenProvider
    {
        private readonly SafeHttpContextAccessor _contextAccessor;

        public HttpClientTokenProvider(SafeHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public string GetToken()
        {
            return _contextAccessor.HttpContext.Request.Cookies[Constants.TOKEN_NAME] ?? throw new UnauthorizedException("HttpClient token not found");
        }
    }
}
