namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class UrlExtensions
    {
        public static string BuildRelativeUrl(this string currentPath, string relPath, int removeSegments = 0)
        {
            if (currentPath is null) throw new ArgumentNullException(nameof(currentPath));
            if (relPath is null) throw new ArgumentNullException(nameof(relPath));

            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(currentPath);
            var noLastSegment = string.Format("{0}://{1}", currentUri.Scheme, currentUri.Authority);

            for (int i = 0; i < currentUri.Segments.Length - removeSegments; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing
            return $"{noLastSegment.Trim("/".ToCharArray())}/{relPath}";
        }
    }
}
