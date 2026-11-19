using System.Text.RegularExpressions;

namespace BOT_Greg_v2_API.Utils;

// the search of the schedule endpoints: case insensitive "contains", plus the shorthands of Constants.SearchAliases
public class SearchQuery
{
    // a variant per shorthand combination is enough, this just stops long searches with many shorthands from exploding
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

    // null if there is nothing to search for (null / empty / whitespace)
    public static SearchQuery? Create(string? search)
    {
        return string.IsNullOrWhiteSpace(search) ? null : new SearchQuery(search.Trim());
    }

    // true if any of the values fits the search
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

        // ignores spaces, dashes etc., so "a tier" and "atier" find "A-Tier"
        // (a search of only symbols, e. g. "-", would match everything after normalizing)
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
                        // the replacement is literal, "$" in a name must not be read as a group reference
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
