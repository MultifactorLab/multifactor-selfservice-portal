namespace MultiFactor.SelfService.Linux.Portal.Core.Caching
{
    public class CachedItem<T>
    {
        public static CachedItem<T> Empty => new CachedItem<T>();

        public T Value { get; set; }
        
        public bool IsEmpty { get; private set; }

        private CachedItem()
        {
            IsEmpty = true;
        }

        public CachedItem(T value)
        {
            IsEmpty = value is null;
            Value = value ?? default(T);
        }
    }
}
