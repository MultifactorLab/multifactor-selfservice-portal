namespace MultiFactor.SelfService.Linux.Portal.Extensions;

public static class StringExtensions
{
    public static List<string> SplitCsv(this string csvList)
    {
        if (string.IsNullOrWhiteSpace(csvList))
            return null;

        return csvList
            .TrimEnd(',')
            .Split(',')
            .Select(s => s.Trim())
            .ToList();
    }
}