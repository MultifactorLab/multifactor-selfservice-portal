namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    internal class HttpMessageInterceptor : DelegatingHandler
    {
        private readonly ILogger<HttpMessageInterceptor> _logger;

        public HttpMessageInterceptor(ILogger<HttpMessageInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Sending request to API: {Method:l} {Uri:l}", request.Method, request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
