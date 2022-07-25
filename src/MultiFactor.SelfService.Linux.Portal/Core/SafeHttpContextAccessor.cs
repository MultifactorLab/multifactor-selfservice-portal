namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public class SafeHttpContextAccessor
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public SafeHttpContextAccessor(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public HttpContext HttpContext => _contextAccessor?.HttpContext ?? throw new Exception("HttpContext can't be null here");
    }
}
