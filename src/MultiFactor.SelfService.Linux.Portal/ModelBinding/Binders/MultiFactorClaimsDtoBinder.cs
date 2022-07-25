using Microsoft.AspNetCore.Mvc.ModelBinding;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders
{
    public class MultiFactorClaimsDtoBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var saml = bindingContext.ValueProvider.GetValue(MultiFactorClaims.SamlSessionId);
            var oidc = bindingContext.ValueProvider.GetValue(MultiFactorClaims.OidcSessionId);

            var model = new MultiFactorClaimsDto(saml.FirstValue ?? string.Empty, oidc.FirstValue ?? string.Empty);
            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
