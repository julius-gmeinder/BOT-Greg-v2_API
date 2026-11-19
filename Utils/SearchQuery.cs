using System.Text.RegularExpressions;

namespace BOT_Greg_v2_API.Utils;

public class SearchQuery
{
    // stops long searches with many shorthands from exploding
    private const int MaxVariants = 16;

    private readonly string _search;
    private readonly string _normalizedSearch;
    private readonly List<Regex> _aliasPatterns;

    private SearchQuery(string search)
    {
        _search = search;
        _normalizedSearch = Normalize(search);
        _aliasPatterns = BuildAliasVariants(search)
            .Select(variant => new Regex($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(variant)}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToList();
    }

    // null if the search is empty
    public static SearchQuery? Create(string? search)
    {
        return string.IsNullOrWhiteSpace(search) ? null : new SearchQuery(search.Trim());
    }

    public bool Fits(params string?[] values)
    {
        return values.Any(Fits);
    }

    private bool Fits(string? value)
    {
        if (value == null)
            return false;

        if (value.Contains(_search, StringComparison.OrdinalIgnoreCase))
            return true;

        // ignores spaces, dashes etc. ("a tier" finds "A-Tier"), a search of only symbols would match everything
        if (_normalizedSearch.Length > 0 && Normalize(value).Contains(_normalizedSearch, StringComparison.OrdinalIgnoreCase))
            return true;

        // the variants have to start at the beginning of a word, otherwise short ones like "EG" would be found in "Legacy"
        return _aliasPatterns.Any(pattern => pattern.IsMatch(value));
    }

    // "iem navi" => "Intel Extreme Masters navi", "iem Natus Vincere", "Intel Extreme Masters Natus Vincere"
    private static List<string> BuildAliasVariants(string search)
    {
        List<string> variants = new() { search };

        foreach (var group in Constants.SearchAliases.Groups)
        {
            foreach (var variant in variants.ToList())
            {
                foreach (var name in group)
                {
                    var namePattern = new Regex($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(name)}(?![\p{{L}}\p{{N}}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                    if (!namePattern.IsMatch(variant))
                        continue;

                    foreach (var otherName in group.Where(other => other != name))
                    {
                        // literal replacement, so "$" in a name is not read as a group reference
                        string replaced = namePattern.Replace(variant, _ => otherName);

                        if (variants.Count < MaxVariants && !variants.Contains(replaced, StringComparer.OrdinalIgnoreCase))
                            variants.Add(replaced);
                    }
                }
            }
        }

        // the search itself is already checked with "contains"
        variants.RemoveAt(0);
        return variants;
    }

    private static string Normalize(string text)
    {
        return new string(text.Where(char.IsLetterOrDigit).ToArray());
    }
}
