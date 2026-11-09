namespace JodVyapar.Api.Common.Text;

/// <summary>
/// The one place keywords are normalized (lowercase, trimmed, inner spaces collapsed, split on commas,
/// de-duplicated in first-seen order). Products, Requirements and later Matching all go through it, so
/// the same words always compare equal.
/// </summary>
public static class KeywordNormalizer
{
    public const int MaxKeywords = 10;
    public const int MaxKeywordLength = 40;

    private const char Separator = ',';

    /// <summary>Accepts chips ("cotton", "yarn"), comma-separated text ("cotton, yarn") or a mix. Never returns null.</summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string?>? values)
    {
        if (values is null)
        {
            return [];
        }

        var keywords = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in values.Where(v => v is not null).SelectMany(v => v!.Split(Separator)))
        {
            var keyword = string.Join(' ', part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
            if (keyword.Length > 0 && seen.Add(keyword))
            {
                keywords.Add(keyword);
            }
        }

        return keywords;
    }

    /// <summary>The value for the Keywords column: comma-separated, or null when there are none.</summary>
    public static string? ToStored(IReadOnlyList<string> keywords) =>
        keywords.Count == 0 ? null : string.Join(Separator, keywords);

    public static IReadOnlyList<string> FromStored(string? stored) =>
        string.IsNullOrEmpty(stored) ? [] : stored.Split(Separator);
}
