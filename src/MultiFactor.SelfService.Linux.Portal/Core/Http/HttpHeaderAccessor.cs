namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    public class HttpHeaderAccessor
    {
        private readonly HttpContext _httpContext;

        public HttpHeaderAccessor(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }
        
        public T GetHeaderValueAs<T>(string headerName) 
        {
            var headers = _httpContext?.Request?.Headers;
            if (headers == null)
                return default(T);
            
            var isHeaderPresent = headers.TryGetValue(headerName, out var values);
            if (isHeaderPresent != true) 
                return default(T);
            
            var rawValues = values.ToString();

            if (!string.IsNullOrEmpty(rawValues))
                return (T)Convert.ChangeType(values.ToString(), typeof(T));
            return default(T);
        }
    }
}