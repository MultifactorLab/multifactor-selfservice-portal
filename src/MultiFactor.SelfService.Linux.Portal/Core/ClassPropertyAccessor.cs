using System.Linq.Expressions;

namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public static class ClassPropertyAccessor
    {
        public static string GetPropertyPath<TClass, TProperty>(Expression<Func<TClass, TProperty>> propertySelector, string separator = ":") where TClass : class
        {
            ArgumentNullException.ThrowIfNull(propertySelector);
            ArgumentNullException.ThrowIfNull(separator);
            if (propertySelector.Body.NodeType != ExpressionType.MemberAccess) throw new Exception("Invalid property name");

            var path = propertySelector.ToString().Split('.').Skip(1) ?? Array.Empty<string>();
            return string.Join(separator, path);
        }
    }
}
