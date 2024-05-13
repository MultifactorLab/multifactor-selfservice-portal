using System.Diagnostics.CodeAnalysis;

namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public class OrdinalIgnoreCaseStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return obj.GetHashCode();
        }
    }
}
