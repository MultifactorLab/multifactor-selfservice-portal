using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MultiFactor.SelfService.Linux.Portal.ModelBinding
{
    public class ModelBinderProviderFactory
    {
        private readonly List<ModelBinderPair> _pairs = new();

        public ModelBinderProviderFactory AddBinding<TModel, TBinder>() where TBinder : IModelBinder
        {
            _pairs.Add(ModelBinderPair.Create<TModel, TBinder>());
            return this;
        }

        public IModelBinderProvider BuildProvider()
        {
            return new PortalBinderProvider(_pairs);
        }
    }
}
