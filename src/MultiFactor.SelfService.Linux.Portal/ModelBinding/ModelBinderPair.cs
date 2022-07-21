using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MultiFactor.SelfService.Linux.Portal.ModelBinding
{
    public class ModelBinderPair
    {
        public Type ModelType { get; }
        public Type BinderType { get; }

        private ModelBinderPair(Type model, Type binder)
        {
            ModelType = model ?? throw new ArgumentNullException(nameof(model));
            BinderType = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public static ModelBinderPair Create<TModel, TBinder>() where TBinder : IModelBinder
        {
            return new(typeof(TModel), typeof(TBinder));
        }
    }
}
