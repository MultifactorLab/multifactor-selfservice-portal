using Microsoft.AspNetCore.Mvc.ModelBinding;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Services.Api;

namespace MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders
{
    public class MultiFactorClaimsDtoBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var saml = bindingContext.ValueProvider.GetValue(MultiFactorClaims.SamlSessionId);
            var oidc = bindingContext.ValueProvider.GetValue(MultiFactorClaims.OidcSessionId);

            var model = new MultiFactorClaimsDto(
                saml != ValueProviderResult.None ? saml.FirstValue : null,
                oidc != ValueProviderResult.None ? oidc.FirstValue : null
                );

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
