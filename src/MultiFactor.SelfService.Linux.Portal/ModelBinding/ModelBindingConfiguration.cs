using Microsoft.AspNetCore.Mvc.ModelBinding;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;

namespace MultiFactor.SelfService.Linux.Portal.ModelBinding
{
    public static class ModelBindingConfiguration
    {
        public static IModelBinderProvider GetModelBinderProvider()
        {
            return new ModelBinderProviderFactory()
                .AddBinding<SingleSignOnDto, MultiFactorClaimsDtoBinder>()
                .BuildProvider();
        }
    }
}
