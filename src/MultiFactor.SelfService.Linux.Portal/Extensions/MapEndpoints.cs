using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class EndpointMapping
    {
        public static void MapApiEndpoints(this IEndpointRouteBuilder builder)
        {
            builder.MapGet(
                "/api/ping", 
                [AllowAnonymous] async (GetApplicationInfoStory getApplicationInfo) => await getApplicationInfo.ExecuteAsync());
        }
    }
}
