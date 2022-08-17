using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    public class SafeHttpContextAccessor
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public SafeHttpContextAccessor(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Returns the HttpContext instance from the current request scope or throws Exception if HttpContext instance is null.
        /// </summary>
        /// <exception cref="HttpContextNotDefinedException"></exception>
        public HttpContext HttpContext => _contextAccessor?.HttpContext ?? throw new HttpContextNotDefinedException("HttpContext can't be null here");
    }
}
