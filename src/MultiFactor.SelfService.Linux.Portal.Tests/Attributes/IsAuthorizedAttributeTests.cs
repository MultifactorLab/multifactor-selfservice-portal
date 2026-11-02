using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Attributes;

public class IsAuthorizedAttributeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PasswordRequirement_DependsOnCurrentTokenRatherThanAnotherLoginCache(bool mustChange)
    {
        var tokens = new PasswordChangeTokens();
        var cache = new Mock<IApplicationCache>();
        cache.Setup(x => x.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(PasswordChangeTokens.Username)))
            .Returns(new CachedItem<string>(PasswordChangeTokens.Username));
        using var services = new ServiceCollection().AddSingleton(tokens.CreateVerifier(new PortalSettings()))
            .AddSingleton(cache.Object).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Headers.Cookie = $"{Constants.COOKIE_NAME}={tokens.Create(mustChange)}";
        var context = new AuthorizationFilterContext(new ActionContext(http, new RouteData(),
            new ActionDescriptor(), new ModelStateDictionary()), new List<IFilterMetadata>());

        new IsAuthorizedAttribute().OnAuthorization(context);

        if (mustChange)
        {
            var result = Assert.IsType<RedirectToRouteResult>(context.Result);
            Assert.Equal("Change", result.RouteValues!["action"]);
        }
        else
        {
            Assert.Null(context.Result);
        }
        cache.Verify(x => x.Get(It.IsAny<string>()), Times.Never);
    }
}
