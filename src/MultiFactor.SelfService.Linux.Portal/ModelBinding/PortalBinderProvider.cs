using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace MultiFactor.SelfService.Linux.Portal.ModelBinding
{
    public class PortalBinderProvider : IModelBinderProvider
    {
        private readonly IReadOnlyList<ModelBinderPair> _binderPairs;

        public PortalBinderProvider(IReadOnlyList<ModelBinderPair> binderPairs)
        {
            _binderPairs = binderPairs;
        }

        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) return null;

            var binderType = _binderPairs.FirstOrDefault(x => x.ModelType == context.Metadata.ModelType)?.BinderType;
            if (binderType == null) return null;

            return new BinderTypeModelBinder(binderType);
        }
    }
}
