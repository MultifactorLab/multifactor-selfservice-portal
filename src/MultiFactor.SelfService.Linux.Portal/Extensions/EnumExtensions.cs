using System.ComponentModel;
using System.Reflection;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class EnumExtensions
    {
        public static string GetEnumDescription(this Enum enumValue)
        {
            var str = enumValue.ToString();
            var fieldInfo = enumValue.GetType().GetField(str);
            var attr = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? str;
        }
    }
}
