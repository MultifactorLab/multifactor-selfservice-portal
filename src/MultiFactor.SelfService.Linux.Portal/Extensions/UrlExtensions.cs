﻿namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class UrlExtensions
    {
        public static string BuildRelativeUrl(this string currentPath, string relPath, int removeSegments = 0)
        {
            ArgumentNullException.ThrowIfNull(currentPath);
            ArgumentNullException.ThrowIfNull(relPath);

            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(currentPath);
            var noLastSegment = $"{currentUri.Scheme}://{currentUri.Authority}";

            for (int i = 0; i < currentUri.Segments.Length - removeSegments; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing
            return $"{noLastSegment.Trim("/".ToCharArray())}/{relPath}";
        }
        
        public static string BuildPostbackUrl(this string documentUrl)
        {
            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(documentUrl);
            var noLastSegment = $"{currentUri.Scheme}://{currentUri.Authority}";

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing /
            noLastSegment = noLastSegment.Trim("/".ToCharArray());

            var postbackUrl = noLastSegment + "/PostbackFromMfa";
            return postbackUrl;
        }
    }
}
