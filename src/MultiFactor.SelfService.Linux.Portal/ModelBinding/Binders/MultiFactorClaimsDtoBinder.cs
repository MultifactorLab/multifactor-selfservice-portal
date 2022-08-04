using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Dto;

namespace MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders
{
    public class MultiFactorClaimsDtoBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var saml = bindingContext.ValueProvider.GetValue(Constants.MultiFactorClaims.SamlSessionId);
            var oidc = bindingContext.ValueProvider.GetValue(Constants.MultiFactorClaims.OidcSessionId);

            var model = new SingleSignOnDto(saml.FirstValue ?? string.Empty, oidc.FirstValue ?? string.Empty);
            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
        
        public static SingleSignOnDto FromRequest(HttpRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            request.Query.TryGetValue(Constants.MultiFactorClaims.SamlSessionId, out StringValues saml);
            request.Query.TryGetValue(Constants.MultiFactorClaims.OidcSessionId, out StringValues oidc);

            return new SingleSignOnDto(saml.FirstOrDefault() ?? string.Empty, oidc.FirstOrDefault() ?? string.Empty);
        }
    }
}
